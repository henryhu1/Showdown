using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAnimatable
{
    public delegate void AnimatableDelegate();

    public void StartAnimation(AnimatableDelegate animationFunction);
}
