/*******************************************************************************
* 作者名称：robin
* 描述：用于简单线性timeline动画
******************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum TimelineDirection
{
    Forward,
    Backward
}


public class Timeline : MonoBehaviour
{

    public Dictionary<string, AnimationCurve> curveTracks = new Dictionary<string, AnimationCurve>();

    public float length = 1f;

    bool bLooping;

    float playRate = 1f;

    float position;

    float lastPosition;

    bool stopAtTargetPos = false;

    float targetPos = 0f;

    bool isPlaying;

    TimelineDirection direction = TimelineDirection.Forward;

    public class TimelineEvent : UnityEvent { };
    TimelineEvent updateEvent = new TimelineEvent();
    TimelineEvent finishEvent = new TimelineEvent();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            float newPosition = position;
            if (direction == TimelineDirection.Forward)
            {
                newPosition += playRate * Time.deltaTime;
                if (newPosition < length)
                {
                    position = newPosition;
                    updateEvent.Invoke();
                    if (stopAtTargetPos && AtPosition(targetPos))
                    {
                        Stop();
                    }
                    lastPosition = position;
                    return;
                }
                position = length;
                updateEvent.Invoke();
                if (bLooping)
                {
                    SetPosition(0f);
                    return;
                }
                isPlaying = false;
                finishEvent.Invoke();
            }
            else
            {
                newPosition -= playRate * Time.deltaTime;
                if (newPosition > 0)
                {
                    position = newPosition;
                    updateEvent.Invoke();
                    if (stopAtTargetPos && AtPosition(targetPos))
                    {
                        Stop();
                    }
                    lastPosition = position;
                    return;
                }
                position = 0f;
                updateEvent.Invoke();
                if (bLooping)
                {
                    SetPosition(length);
                    return;
                }
                isPlaying = false;
                finishEvent.Invoke();
            }
        }
    }

    public void Play()
    {
        direction = TimelineDirection.Forward;
        isPlaying = true;
    }

    public void PlayFromStart()
    {
        isPlaying = false;
        position = 0f;
        lastPosition = 0f;
        direction = TimelineDirection.Forward;
        isPlaying = true;
        updateEvent.Invoke();
    }

    public void PlayTo(float pos)
    {
        isPlaying = false;
        bLooping = false;
        targetPos = pos;
        if (targetPos < position)
        {
            direction = TimelineDirection.Backward;
        }
        else
        {
            direction = TimelineDirection.Forward;
        }
        stopAtTargetPos = true;
        isPlaying = true;
    }

    public void Reverse()
    {
        direction = TimelineDirection.Backward;
        isPlaying = true;
    }

    public void ReverseFromEnd()
    {
        isPlaying = false;
        position = length;
        lastPosition = length;
        direction = TimelineDirection.Backward;
        isPlaying = true;
        updateEvent.Invoke();
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Resume()
    {
        isPlaying = true;
    }

    public void SetPosition(float newTime)
    {
        position = newTime;
        lastPosition = newTime;
        updateEvent.Invoke();
    }

    public void SetLooping(bool newLooping)
    {
        bLooping = newLooping;
    }

    public void SetPlayRate(float newRate)
    {
        playRate = newRate;
    }

    //添加曲线轨道
    public void AddTrack(string newTrackName, AnimationCurve newCurve)
    {
        if (!curveTracks.ContainsKey(newTrackName))
        {
            curveTracks.Add(newTrackName, newCurve);
        }
        else
        {
            Debug.LogWarning($"字典中已含有名为{newTrackName}的key，已替换对应value");
            curveTracks[newTrackName] = newCurve;
        }
    }

    public float GetTrackValue(string trackName)
    {
        if (curveTracks[trackName] == null)
        {
            return 0f;
        }
        return curveTracks[trackName].Evaluate(position);
    }

    public void AddUpdateEvent(UnityAction newEvent)
    {
        updateEvent.AddListener(newEvent);
    }

    public void AddFinishEvent(UnityAction newEvent)
    {
        finishEvent.AddListener(newEvent);
    }

    public TimelineDirection GetDirection()
    {
        return direction;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public float GetPosition()
    {
        return position;
    }

    //给定一个时间点，判断当前时间轴是否播放到了该时间点
    public bool AtPosition(float pos)
    {
        if (lastPosition < position)
        {
            return lastPosition < pos && pos <= position;
        }
        else
        {
            return position <= pos && pos < lastPosition;
            ;
        }
    }
}
