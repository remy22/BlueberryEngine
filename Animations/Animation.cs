﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blueberry.Animations
{
    public enum LoopMode : byte
    {
        None = 0,
        Loop = 1,
        LoopWithReversing = 2,
        LoopWithReversingInterpolation = 3
    }
    public enum PlaybackState
    {
        Play,
        Pause,
        Stop
    }
	public delegate float Interpolator(float interval);

    public abstract class Animation<T> : IAnimation
    {
        public virtual T From { get; protected set; }
        public virtual T To { get; protected set; }
        public virtual T Value { get; protected set; }
        public LoopMode Loop { get; set; }
        public PlaybackState State { get; private set; }
        public virtual double Period { get; protected set; }
		public Interpolator InterpolationFunction{ get; set; }

        protected bool interpolatorOrientation; // when first time animation direction is true or false, current interpalation should be the same
        protected double timer; // main timer
		protected float interval;
        protected bool direction; // true - move from left to right
        public event Action<Animation<T>> OnFinish;
        public event Action<Animation<T>> OnStart;
        public event Action<Animation<T>, T> OnAnimate;
		public event Action<Animation<T>, T> OnExtremum;
		
        public Animation () : this(v => v)  // linear interpolation;
		{}
        public Animation (Interpolator interpolator)
        {
            State = PlaybackState.Stop;
            InterpolationFunction = interpolator; 
        }

        public virtual void Animate(double dt)
        {
            timer += dt;
            if (timer >= Period)
            {
            	if(OnExtremum != null)
            		OnExtremum(this, Value);
                if (Loop == LoopMode.None)
                {
                    timer = Period;
                    Stop();
                    if(OnFinish != null)
                        OnFinish(this);
                    goto end;
                }
                timer -= Period;
                if (Loop == LoopMode.LoopWithReversing || Loop == LoopMode.LoopWithReversingInterpolation)
                    direction = !direction;
            }
            end:
            interval = (float)(timer / Period);
        }
        protected void RaiseAnimateEvent()
        {
            if(OnAnimate != null)
                OnAnimate(this, Value);
        }
        public virtual void Play(bool restart = false)
        {
            lock (AnimationManager.Manager.updateMutex)
            {
                if (restart)
                {
                    Value = From;
                    timer = 0;
                    State = PlaybackState.Play;
                    AnimationManager.Manager.animations.Add(this);
                    if (OnStart != null)
                        OnStart(this);
                } else
                {
                    if (State == PlaybackState.Pause)
                        Resume();
                    else if (State == PlaybackState.Stop)
                    {
                        Value = From;
                        timer = 0;
                        State = PlaybackState.Play;
                        AnimationManager.Manager.animations.Add(this);
                        if (OnStart != null)
                            OnStart(this);
                    }
                }
            }
        }

        public virtual void Pause()
        {
            lock (AnimationManager.Manager.updateMutex)
            {
                if (State == PlaybackState.Play)
                {
                    State = PlaybackState.Pause;
                    AnimationManager.Manager.animations.Remove(this);
                }
            }
        }

        public virtual void Stop()
        {
            lock (AnimationManager.Manager.updateMutex)
            {
                if (State != PlaybackState.Stop)
                {
                    State = PlaybackState.Stop;
                    AnimationManager.Manager.animations.Remove(this);
                }
            }
        }

        public virtual void Resume()
        {
            lock (AnimationManager.Manager.updateMutex)
            {
                if (State == PlaybackState.Pause)
                {
                    State = PlaybackState.Play;
                    AnimationManager.Manager.animations.Add(this);
                }
            }

        }

        public virtual void Dispose()
        {
            lock (AnimationManager.Manager.updateMutex)
            {
                AnimationManager.Manager.animations.Remove(this);
            }
        }
    }
}
