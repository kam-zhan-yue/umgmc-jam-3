using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class FirePoint : MonoBehaviour
{
    private enum State
    {
        Idle = 0,
        Firing = 1,
        Hooked = 2,
        Reeling = 3,
    }
    [SerializeField] private bool debug;
    [SerializeField] private float destination_tolerance_check;
    [SerializeField] private TentacleMovement point;
    [SerializeField] private List<TentacleMovement> point_list = new List<TentacleMovement>();
    [SerializeField] private Vector3[] point_positions_array;
    [SerializeField] private float speed;
    [SerializeField] private int num_of_points;
    [SerializeField] private float delay_between_firing_points;
    [SerializeField] private LineRenderer line_renderer;
    private Vector3 cursor_position;
    [Header("Lerp Values")]
    [SerializeField] private float duration;
    
    private bool _reachedDestination = false;
    private bool _reachedOrigin = false;
    private float _originalSpeed = 0f;
    private float _originalTolerance = 0f;

    private float _scaleFactor = 0f;

    private State _state = State.Idle;

    private FishingPole _fishingPole;
    
    private void Awake()
    {
        _originalSpeed = speed;
        _originalTolerance = destination_tolerance_check;
        point_positions_array = new Vector3[num_of_points];
        Initialize_Array();
    }

    public void Initialize_Array()
    {
        for (int i=0; i<num_of_points; i++)
        {
            Vector3 direction = (cursor_position - transform.position).normalized;
            // Instantiate a TentacleMovement object at the fire point's position
            TentacleMovement _point = Instantiate(point, transform.position, Quaternion.identity);
            // Initialize the TentacleMovement object
            // set its speed to zero and set it to inactive
            _point.transform.parent = gameObject.transform;
            _point.Initialize_Point(direction, 0 , 0);
            _point.gameObject.SetActive(false);
            _point.OnHook += OnHook;
            _point.OnLetGo += OnLetGo;
            _point.OnReel += OnReel;
            _point.OnAutoHook += OnAutoHook;
            //add to list
            point_list.Add(_point);
        }

        point_list[0].ReachedOrigin += ReachedOrigin;
    }
    

    private void OnHook(IHookable hookable)
    {
        //Pause the tentacle
        _state = State.Hooked;
        Pause();
    }

    private void Pause()
    {
        Debug.Log("Pause");
        for (int i = 0; i < point_list.Count; ++i)
        {
            point_list[i].TogglePause(true);
            point_list[i].SetCanGame(false);
            point_list[i].SetSpeed(0f);
        }
    }

    private void SetCanGame(bool canGame)
    {
        for (int i = 0; i < point_list.Count; ++i)
        {
            point_list[i].SetCanGame(canGame);
        }
    }

    private void SetCanHook(bool canHook)
    {
        for (int i = 0; i < point_list.Count; ++i)
        {
            point_list[i].SetCanHook(canHook);
        }
    }
    
    private void UnPause()
    {
        for (int i = 0; i < point_list.Count; ++i)
        {
            point_list[i].TogglePause(false);
        }
    }

    private void OnLetGo(IHookable hookable)
    {
        SetCanHook(false);
        Debug.Log("FirePoint LetGo");
        UnPause();
        SetSpeed(speed);
        ReturnPoints();
        _state = State.Reeling;
    }

    private void OnReel(IHookable hookable)
    {
        UnPause();
        SetSpeed(speed);
        ReturnPoints();
        _state = State.Reeling;
    }

    //Old implementation
    private void OnAutoHook(IHookable hookable)
    {
        // _state = State.Reeling;
        _reachedDestination = true;
        // ReturnPoints();
    }
    
    private void ReachedOrigin()
    {
        _reachedOrigin = true;
    }

    [Button]
    public void Scale(float scaleFactor)
    {
        _scaleFactor = scaleFactor;
        line_renderer.widthMultiplier = 1 + _scaleFactor * 0.5f;
        for (int i = 0; i < point_list.Count; ++i)
        {
            point_list[i].Scale(scaleFactor);
        }
    }

    private void Update()
    {
        // Check if the left mouse button is pressed down
        if (debug && Input.GetMouseButtonDown(0))
        {
            // Get the cursor position in world space
            cursor_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            cursor_position.z = 0;
            StartCoroutine(DelayBetweenShots(delay_between_firing_points));
        }
        UpdatePositionsArray();
        //set the number of vertices and then assign the points
        line_renderer.positionCount = num_of_points;
        line_renderer.SetPositions(point_positions_array);
    }

    public async UniTask Shoot(Vector3 target, float multiplier, CancellationToken token)
    {
        SetCanGame(true);
        SetCanHook(true);
        UnPause();
        Vector3 position = transform.position;
        Vector3 adjustedTarget = new Vector3(target.x, target.y, position.z);
        Vector3 difference = adjustedTarget - position;
        Vector3 finalPoint = difference * multiplier;
        
        _reachedOrigin = false;
        cursor_position = finalPoint;
        cursor_position.z = 0f;
        speed = UniverseHelper.GetTentacleSpeed(_originalSpeed, _scaleFactor);
        destination_tolerance_check = _originalTolerance * _scaleFactor;
        StartCoroutine(DelayBetweenShots(delay_between_firing_points));
        await UniTask.WaitUntil(() => _reachedOrigin, cancellationToken: token);
    }

    private void SetSpeed(float tentacleSpeed)
    {
        foreach (TentacleMovement _point in point_list)
        {
            _point.SetSpeed(tentacleSpeed);
        }
    }

    private void ReturnPoints()
    {
        if (_state == State.Reeling)
            return;
        for (int i = 0; i < point_list.Count; ++i)
        {
            point_list[i].ReturnAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
        StartCoroutine(LerpFloat(speed,speed*(-1), duration));
    }

    //we will lerp the slow down at the end
    private IEnumerator LerpFloat(float startValue, float endValue, float _duration)
    {
        float currentTime = 0f;

        while (currentTime < _duration)
        {
            // Increment current time using deltaTime from WaitForSeconds
            currentTime += Time.deltaTime;

            // Calculate t value between 0 and 1 based on current time and duration
            float t = currentTime / _duration;

            // Perform the lerp between start and end values
            float lerpedValue = Mathf.Lerp(startValue, endValue, t);

            SetSpeed(lerpedValue);

            yield return null;
        }
    }

    IEnumerator DelayBetweenShots(float delay_between_firing_points)
    {
        _state = State.Firing;
        _reachedDestination = false;
        for (int i = 0; i < num_of_points; i++)
        {
            if (_state == State.Hooked)
            {
                Debug.Log("LOG | Tentacle Hooked! Stop Firing!");
                yield break;
            }
            if (_reachedDestination)
            {
                Debug.Log("Reached Destination");
                ReturnPoints();
                yield break;
            }
            //if its true we no longer need to fire points

            if (i == 0)
            {
                ShootPoint(speed, i, cursor_position);
                CheckDestinationAsync(this.GetCancellationTokenOnDestroy()).Forget();
            }
            else
            {
                ShootPoint(speed, i);
            }
            
            yield return new WaitForSeconds(delay_between_firing_points);
            //line_renderer.SetPositions(point_list);
        }
        
        Debug.Log("All Points Shot");
        //If all points are shot, bring them back in
        ReturnPoints();
    }

    private async UniTask CheckDestinationAsync(CancellationToken token)
    {
        await point_list[0].ReachedDestinationAsync(token);
        _reachedDestination = true;
        // SetCanGame(false);
        // while (_shootingPoints)
        // {
        //     float distanceToDestination = Vector3.Distance(point_list[0].transform.position, cursor_position);
        //     //Debug.Log(distanceToDestination);
        //     if (distanceToDestination<destination_tolerance_check)
        //     {
        //         _reachedDestination = true;
        //         Debug.Log("Reached Destination Async, Lerping Back");
        //         //If reached destination, bring them back in
        //         LerpTheValues();
        //         break;
        //     }
        //     await UniTask.NextFrame(token);
        // }
    }

    public void ShootPoint(float speed, int index, Vector3 destination = default)
    {
        
        Vector3 direction = (cursor_position - transform.position).normalized;

        // Initialize the object that is in this point in the index and set it to active
        AnimationCurve widthCurve = line_renderer.widthCurve;
        int positions = line_renderer.positionCount;
        float positionOnCurve = (float)index / positions;
        float width = widthCurve.Evaluate(positionOnCurve);
        point_list[index].Initialize_Point(direction, speed, width, destination);
        point_list[index].gameObject.SetActive(true);
    }

    public void UpdatePositionsArray()
    {
        int count = 0;

        foreach (TentacleMovement point in point_list )
        {
            /*
            if (point_positions_array[count] == Vector3.zero)
            {
                Debug.Log("Empty point");
                return;
            */
            point_positions_array[count] = point.transform.position;
            count++;
        }
    }

    private void OnDestroy()
    {
        if (point_list.Count > 0)
        {
            point_list[0].ReachedOrigin -= ReachedOrigin;
            for (int i = 0; i < point_list.Count; ++i)
            {
                point_list[i].OnHook -= OnHook;
                point_list[i].OnLetGo -= OnLetGo;
                point_list[i].OnReel -= OnReel;
                point_list[i].OnAutoHook -= OnAutoHook;
            }
        }
    }
}
