// License is the "Unlicense."
// Written by @jonathanczeck
// Enjoy!

using UnityEngine;

public sealed class Ease {
	public static readonly Ease Default = new Ease(InterpolateHermite);
	
	public static readonly Ease Berp = new Ease(InterpolateBerp);
	public static readonly Ease Bounce = new Ease(InterpolateBounce);
	public static readonly Ease Cubic = new Ease(InterpolateCubic);
	public static readonly Ease CubicIn = new Ease(InterpolateCubicIn);
	public static readonly Ease CubicOut = new Ease(InterpolateCubicOut);
	public static readonly Ease Hermite = new Ease(InterpolateHermite);
	public static readonly Ease Instant = new Ease(InterpolateInstant);
	public static readonly Ease JumpAndBounce = new Ease(InterpolateJumpAndBounce);
	public static readonly Ease JumpAndReturnBounce = new Ease(InterpolateJumpAndReturnBounce);
	public static readonly Ease Linear = new Ease(InterpolateLinear);
	public static readonly Ease OvershootIn = new Ease(InterpolateOvershootIn);
	public static readonly Ease OvershootOut = new Ease(InterpolateOvershootOut);
	public static readonly Ease Quadratic = new Ease(InterpolateQuadratic);
	public static readonly Ease QuadraticIn = new Ease(InterpolateQuadraticIn);
	public static readonly Ease QuadraticOut = new Ease(InterpolateQuadraticOut);
	public static readonly Ease Quartic = new Ease(InterpolateQuartic);
	public static readonly Ease QuarticIn = new Ease(InterpolateQuarticIn);
	public static readonly Ease QuarticOut = new Ease(InterpolateQuarticOut);
	public static readonly Ease Quintic = new Ease(InterpolateQuintic);
	public static readonly Ease QuinticIn = new Ease(InterpolateQuinticIn);
	public static readonly Ease QuinticOut = new Ease(InterpolateQuinticOut);
	public static readonly Ease SlamAndReturn = new Ease(InterpolateSlamAndReturn);
	public static readonly Ease SpringAndReturn = new Ease(InterpolateSpringAndReturn);
	public static readonly Ease TouchAndReturn = new Ease(InterpolateTouchAndReturn);
	
	public delegate float EaseAction(float t);
	
	public EaseAction Interpolate {
		get; private set;
	}
	
	public Ease (EaseAction easeAction) {
		Interpolate = easeAction;
	}
	
	public Ease (AnimationCurve animationCurve) {
		this.animationCurve = animationCurve;
		Interpolate = InterpolateAnimationCurve;
	}
	
	private AnimationCurve animationCurve;
	
	float InterpolateAnimationCurve(float x) {
		return animationCurve.Evaluate(x);
	}
	
	public static Ease Custom(AnimationCurve animationCurve) {
		return new Ease(animationCurve);
	}
	
	static float InterpolateBerp(float x) {
		return (Mathf.Sin(x * Mathf.PI * (0.2f + 2.5f * x * x * x)) * Mathf.Pow(1f - x, 2.2f) + x) * (1f + (1.2f * (1f - x)));
	}
	
	static float InterpolateBounce(float x) {
		if (x < (4f/11f)) {
			return (121f/16f) * x * x;
		}
		if (x < (8f/11f)) {
			return (121f/16f) * (x - (6f/11f)) * (x - (6f/11f)) + 0.75f;
		}
		if (x < (10f/11f)) {
			return (121f/16f) * (x - (9f/11f)) * (x - (9f/11f)) + (15f/16f);
		}
		return (121f/16f) * (x - (21f/22f)) * (x - (21f/22f)) + (63f/64f);
	}
	
	static float InterpolateCubic(float x) {
		if (x < 0.5f) {
			return 4f * x * x * x;
		} else {
			x = 2 * x - 2;
			return 0.5f * x * x * x + 1f;
		}
	}
	
	static float InterpolateCubicIn(float x) {
		return x * x * x;
	}
	
	static float InterpolateCubicOut(float x) { 
		x = x - 1f;
		return x * x * x + 1f;
	}
	
	static float InterpolateHermite(float x) {
		return x * x * (3.0f - 2.0f * x);
	}
	
	static float InterpolateInstant(float x) {
		return 1f;
	}
	
	static float InterpolateJumpAndReturnBounce(float x) {
		if (x < 0.5333f) {
			x = (15f/11f) * x - (4f/11f);
			return 1f - ((121f/16f) * x * x);
		}
		if (x < 0.8f) {
			x = (15f/11f) * x - (10f/11f);
			return 1f - ((121f/16f) * x * x + 0.75f);
		}
		if (x < 0.933f) {
			x = (15f/11f) * x - (13f/11f);
			return 1f - ((121f/16f) * x * x + (15f/16f));
		}
		x = (15f/11f) * x - (29f/22f);
		return 1f - ((121f/16f) * x * x + (63f/64f));
	}
	
	static float InterpolateJumpAndBounce(float x) {
		if (x < 0.5532f) {
			x = (2.851131111f * x - 1f);
			return 1.5f - 1.5f * x * x;
		}
		if (x < 0.7871f) {
			x = (2.851131111f * x - 1.91068f);
			return 1.1666666f - 1.5f * x * x;
		}
		if (x < 0.9221f) {
			x = (2.851131111f * x - 2.4364634f);
			return 1.0555555f - 1.5f * x * x;
		}
		
		x = (2.851131111f * x - 2.74002f);
		return 1.0185185185f - 1.5f * x * x;
	}
	
	static float InterpolateLinear(float x) {
		return x;
	}
	
	static float InterpolateOvershootIn(float x) {
		return x * x * (2.70158f * x - 1.70158f);
	}
	
	static float InterpolateOvershootOut(float x) {
		x -= 1f;
		return x * x * (2.70158f * x + 1.70158f) + 1f;
	}
	
	static float InterpolateQuadratic(float x) {
		if (x < 0.5f) {
			return 2f * x * x;
		} else {
			x = 2 * x - 1;
			return -0.5f * x * x + x + 0.5f;
		}
	}
	
	static float InterpolateQuadraticIn(float x) {
		return x * x;
	}
	
	static float InterpolateQuadraticOut(float x) {
		return (2f - x) * x;
	}
	
	static float InterpolateQuartic(float x) {
		if (x < 0.5f) {
			return 2f * x * x;
		} else {
			x = 2 * x - 2;
			return 1f - 0.5f * x * x * x * x;
		}
	}
	
	static float InterpolateQuarticIn(float x) {
		return x * x * x * x;
	}
	
	static float InterpolateQuarticOut(float x) {
		x -= 1f;
		return 1f - x * x * x * x;
	}
	
	static float InterpolateQuintic(float x) {
		if (x < 0.5f) {
			return 16f * x * x * x * x * x + 1f;
		} else {
			x = 2f * x - 2f;
			return 0.5f * x * x * x * x * x + 2f;
		}
	}
	
	static float InterpolateQuinticIn(float x) {
		return x * x * x * x * x;
	}
	
	static float InterpolateQuinticOut(float x) {
		x -= 1f;
		return x * x * x * x * x;
	}
	
	static float InterpolateSlamAndReturn(float x) {
		if (x < 0.125f) {
			return Mathf.Sin(12.5663706f * x);
		} else {
			return 0.5f * Mathf.Cos(3.5475f * (x - 0.125f)) + 0.5f;
		}
	}
	
	static float InterpolateSpringAndReturn(float x) {
		return (2f * Mathf.Pow(2f, -10f * x) * Mathf.Sin(x * 25f));
	}
	
	static float InterpolateTouchAndReturn(float x) {
		return 6.75f * x * (1f - x) * (1f - x);
	}
	
}
