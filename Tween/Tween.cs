// License is the "Unlicense."
// Written by @jonathanczeck
// Enjoy!

/*
	The Tween class is for animating around Transforms and other values.
	You queue up little "programs" for the Tween class to execute, then
	call Run() to start the animations.
	
	Example 1, Basic usage:
	
		Tween.Begin(transform);
		Tween.Move(new Vector3(3f, 0f, 0f));
		Tween.Run();
		
		This will animate the local position of the transform to (3,0,0)
		in local space. A Tween program begins with
		Tween.Begin(theTransform). You can call Tween.Begin() again to
		queue up another program. Multiple programs can be queued up and
		then Tween.Run() at once. Tween.Run() will run all the programs
		queued up and clear the queue.

	Example 2, Setting defaults:
	
		Tween.Begin(transform);
		Tween.Duration = 3f;
		Tween.Space = Space.World;
		Tween.Easing = Ease.Berp;
		Tween.Rotate(new Vector3(0f, 360f, 0f));
		Tween.Run();
		
		You may set the Space, Duration, and Ease that the subsequent
		commands in the Tween program will use. When you Tween.Begin()
		again, they are reset.
	
	Example 3, Specifying directly instead of setting defaults:
	
		Tween.Begin(transform);
		Tween.Scale(Vector3.one * 1.5f, Ease.Bounce);
		Tween.Move(Vector3.up * 5f, Space.World, 2f, Ease.JumpAndBounce);
		Tween.Run();
		
		You may optionally specify Space, Duration, and Ease directly
		instead of setting the defaults of the program. There are
		multiple overloads for many of these functions so that you may
		specify only what you want to.
		
	Example 4, Running multiple Tween programs in parallel:
	
		Tween.Begin(transform);
		Tween.Move(Vector3.up * 2f, Ease.CubicOut);		
		Tween.Begin(otherTransform);
		Tween.MoveRelativeTo(transform, Space.World, Vector3.forward);
		Tween.Run();
		
		This will perform both programs at the same time so both
		Transforms will move at the same time.
		
	Example 5, Inserting a pause and parenting:
	
		Tween.Begin(transform);
		Tween.Move(new Vector3(0f, 2f, 0f), 10f);
		Tween.Delay(1f);
		Tween.Rotate(Quaternion.Euler(90f, 0f, 0f));
		Tween.Scale(Vector3.one * 0.5f, 2f);
		Tween.Flush();
		Tween.Parent(newParentTransform);
		Tween.Move(Vector3.zero);
		Tween.Run();
		
		This will move the transform to (0,2,0), wait a second,
		rotate and scale the transform, then wait until the previous
		instructions complete. Finally, it will set a new parent to
		the transform and move the transform back to (0,0,0) local
		space. Tween.Flush() is nearly identical to Tween.Delay(),
		except you don't have to think about what duration to pass.
	
	Example 6, Using Tween.Custom():
	
		Tween.Begin(transform);
		Tween.Custom(10f, factor => {
			renderer.material.color = Color.Lerp(Color.red, Color.blue, factor);
		});
		Tween.Move(new Vector3(5f, 0f, 0f), Space.World);
		Tween.Delay(2f);
		Tween.Custom(3f, MyFunctionThatTakesInAFloat);
		yield return Tween.Run().Coroutine;
		Debug.Log("Tada!");
		
	Example 7, Using the Tween.Job class and Tween.Run() options:
	
		Tween.Begin(transform);
		Tween.Move(Vector3.one);
		Tween.Job tweenJob = Tween.Run(ignoreTimeScale : true, onFinished : name => {Debug.Log("Finished!");});
		yield return tweenJob.Coroutine;
		Debug.Log("All tweening programs completed!");
		
		Tween.Begin(transform);
		Tween.Move(Vector3.zero);
		tweenJob = Tween.Run(onCancelled : name => {Debug.Log("Cancelled!");});
		while (tweenJob.IsRunning) {
			if (Input.GetKeyDown(KeyCode.Space)) {
				tweenJob.Cancel();
			}
			if (Input.GetKeyDown(KeyCode.P)) {
				tweenJob.IsPaused = !tweenJob.IsPaused;
			}
			yield return null;
		}
		
	Example 8, Using Tween.Call():
	
		public class CallTest : MonoBehaviour {
			public Transform prefab;
			
			void Start () {
				Tween.Begin (transform);
				Tween.Move(new Vector3(10f, 0f, 0f), 0f);
				Tween.Delay (3f);
				Tween.Call(Hi);
				Tween.Move (Vector3.zero, 3f);
				Tween.Flush ();
				Tween.Call (() => {
					Instantiate(prefab);
					Talk("Hello to you, too!");
				});
				Tween.Delay (3f);
				Tween.Move (Vector3.one);
				Tween.Flush();
				Tween.Call (() => { Debug.Log ("Thanks"); });
				Tween.Run();
			}
			
			void Hi () {
				Debug.Log ("¿What's up dawg?");
			}
			
			void Talk(string message) {
				Debug.Log(message);
			}
		}
	
	You can do some pretty cool animations with this Tween class
	without thinking too hard. Need any conveniences? Find some bugs?
	Talk to Jon. Have fun and remember to perform your monthly safety
	check on your garage door, if applicable.
*/


using System.Collections.Generic;
using System.Collections;
using System;
// UnityEngine is not being included so we can be cool and have the property Tween.Space while still being in the default namespace.

public sealed class Tween : UnityEngine.MonoBehaviour {
#region Public Interface
	/// <summary>
	/// A Tween program begins with Tween.Begin(). Call this first. Pass a specified Transform, then add additional lines of any number of instructions such as Tween.Move(), set the defaults such as Tween.Space. You may also Tween.Begin(anotherTransform) to have another program run paralell with this one. After all your desired instructions, call Tween.Run() to start the animation Job. This will execute your commands over time and clear out the programs so you can Tween.Begin() fresh again.
	/// </summary>
	/// <param name="target">Which Transform the following Tween instructions will apply to.</param>
	public static void Begin(UnityEngine.Transform target) {
		if (instance == null) {
			UnityEngine.GameObject go = new UnityEngine.GameObject("Tween Manager", typeof(Tween));
			go.hideFlags = UnityEngine.HideFlags.HideInHierarchy;
			UnityEngine.Object.DontDestroyOnLoad(go);
			instance = go.GetComponent<Tween>();
		}
		
		currentProgram = new Program(target);
		programList.Add(currentProgram);
	}
	
	/// <summary>
	/// Gets or sets the default Space to either Space.Self or Space.World. Then, for the subsequent instructions until a Begin() or Run() command, instructions without a Space specified will default to this value. This property defaults to Space.Self.
	/// </summary>
	/// <value>The Space value, defaulting to Space.Self</value>
	public static UnityEngine.Space Space {
		get {
			return currentProgram.defaultSpace;
		}
		set {
			currentProgram.defaultSpace = value;
		}
	}
	
	/// <summary>
	/// Gets or sets the Duration for subsequent Tween instructions until a Begin() or Run() command. The Duration is the length of time in seconds an instruction interpolates over. This property defaults to 1/3 second which is a good length of time for a snappy transistion.
	/// </summary>
	/// <value>The Duration in seconds.</value>
	public static float Duration {
		get {
			return currentProgram.defaultDuration;
		}
		set {
			currentProgram.defaultDuration = value;
		}
	}
	
	/// <summary>
	/// Gets or sets the default Ease (Easing) for subsequent Tween instructions until a Begin() or Run() command. An Ease defines how a value moves from a start value to an end value over time. The most simple useful Ease is Ease.Linear. This property defaults to Ease.Default, which the same as Ease.Hermite.
	/// </summary>
	/// <value>An Ease class instance such as Ease.Cubic.</value>
	public static Ease Easing {
		get {
			return currentProgram.defaultEase;
		}
		set {
			currentProgram.defaultEase = value;
		}
	}
	
	/// <summary>
	/// Gets or sets the default color for subsequent Tween instructions until a Begin() or Run() command. Color is used for the Fade() and FadeAlpha() instructions. This property defaults to Color.white.
	/// </summary>
	/// <value>The UnityEngine.Color.</value>
	public static UnityEngine.Color Color {
		get {
			return currentProgram.defaultColor;
		}
		set {
			currentProgram.defaultColor = value;
		}
	}
	
	/// <summary>
	/// Delay the specified duration in seconds before executing subsequent instructions within a Tween program.
	/// </summary>
	public static void Delay(float duration) {
		currentProgram.instructions.Add(Instruction.Delay(duration));
	}
	
	/// <summary>
	/// Delay the default Tween.Duration before executing subsequent instructions within a Tween program.
	/// </summary>
	public static void Delay() {
		currentProgram.instructions.Add(Instruction.Delay(currentProgram.defaultDuration));
	}
	
	/// <summary>
	/// Similar to Tween.Delay(), this waits for previous instructions in the Tween program to finish before executing subsequent instructions.
	/// </summary>
	public static void Flush() {
		List<Instruction> instructionList = currentProgram.instructions;
		
		if (instructionList.Count > 0) {
			float maxDuration = 0f;

			for (int index = instructionList.Count - 1; index >= 0; index--) {
				Instruction instruction = instructionList[index];
				
				if ((instruction.Opcode & Operation.Flush) != Operation.None) {
					break;
				}
				
				if ((instruction.Opcode & (Operation.Delay | Operation.Parent)) == Operation.None) {
					if (instruction.Duration > maxDuration) {
						maxDuration = instruction.Duration;
					}
				}
			}
			
			if (maxDuration > 0f) {
				instructionList.Add(Instruction.Flush (maxDuration));
			}
		}
	}
	
	/// <summary>
	/// Set the parent of this program's target Transform to the specified new parent Transform.
	/// </summary>
	public static void Parent(UnityEngine.Transform newParent) {
		currentProgram.instructions.Add (Instruction.Parent(newParent));
	}

	/// <summary>
	/// Add a Custom instruction to the Tween program. A Custom instruction is a delegate that takes in a float that is in the range of 0..1, and then does something interesting with that. The delegate is called every frame, similar to a regular Update() function. This could be used to, for example, animate an arbitrary script value. Values for Duration and Easing may optionally be specified.
	/// </summary>
	public static void Custom(System.Action<float> customInstruction) {
		currentProgram.instructions.Add(Instruction.Custom(customInstruction, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Custom(float duration, System.Action<float> customInstruction) {
		currentProgram.instructions.Add(Instruction.Custom(customInstruction, duration, currentProgram.defaultEase));
	}
	
	public static void Custom(Ease easing, System.Action<float> customInstruction) {
		currentProgram.instructions.Add(Instruction.Custom(customInstruction, currentProgram.defaultDuration, easing));
	}
		
	public static void Custom(float duration, Ease easing, System.Action<float> customInstruction) {
		currentProgram.instructions.Add(Instruction.Custom(customInstruction, duration, easing));
	}
	
	#region Move
	/// <summary>
	/// Move this program's target Transform to the specified position. You may also optionally specify a Space, Duration, and Ease type.
	/// </summary>
	public static void Move(UnityEngine.Vector3 toPosition = default(UnityEngine.Vector3)) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, UnityEngine.Space space) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, float duration) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, duration, currentProgram.defaultEase));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, currentProgram.defaultDuration, easing));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, UnityEngine.Space space, float duration) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, duration, currentProgram.defaultEase));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, UnityEngine.Space space, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, currentProgram.defaultDuration, easing));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, float duration, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, duration, easing));
	}
	
	public static void Move(UnityEngine.Vector3 toPosition, UnityEngine.Space space, float duration, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.Move, toPosition, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, duration, easing));
	}
	#endregion
	#region MoveRelativeTo
	/// <summary>
	/// Moves the world position of this program's target Transform to an offset relative to the specified relativeTo Transform. You may also optionally specify what Space the offset should use, a Duration, and an Ease type.
	/// </summary>
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, UnityEngine.Space offsetSpace) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, offsetSpace, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, float duration) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, currentProgram.defaultSpace, duration, currentProgram.defaultEase));
	}
	
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, currentProgram.defaultSpace, currentProgram.defaultDuration, easing));
	}
	
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, UnityEngine.Space offsetSpace, float duration) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, offsetSpace, duration, currentProgram.defaultEase));
	}
	
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, UnityEngine.Space offsetSpace, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, offsetSpace, currentProgram.defaultDuration, easing));
	}
	
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, float duration, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, currentProgram.defaultSpace, duration, easing));
	}
	
	public static void MoveRelativeTo(UnityEngine.Transform relativeTo, UnityEngine.Vector3 offset, UnityEngine.Space offsetSpace, float duration, Ease easing) {
		currentProgram.instructions.Add(new Instruction(Operation.MoveRelativeTo, offset, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, relativeTo, offsetSpace, duration, easing));
	}
	#endregion
	#region Rotate (Quaternion)
	/// <summary>
	/// Rotates this program's target Transform to the specified rotation expressed as a Quaternion. You may optionally specify what Space the target should rotate in, the Duration, and an Ease type.
	/// </summary>
	public static void Rotate(UnityEngine.Quaternion toRotation) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, UnityEngine.Space space, float duration, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, duration, easing));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, UnityEngine.Space space) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, float duration) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, duration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, currentProgram.defaultDuration, easing));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, UnityEngine.Space space, float duration) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, duration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, UnityEngine.Space space, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, space, currentProgram.defaultDuration, easing));
	}
	
	public static void Rotate(UnityEngine.Quaternion toRotation, float duration, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.Rotate, UnityEngine.Vector3.zero, toRotation, UnityEngine.Vector3.zero, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, duration, easing));
	}
	#endregion
	#region Rotate (Vector3)
	/// <summary>
	/// Rotates this program's target Transform to the specified rotation expressed as a Vector3. This will allow you to rotate something by a certain number of degrees instead of interpolating to another orientation directly. You may optionally specify what Space the target should rotate in, the Duration, and an Ease type.
	/// </summary>
	public static void Rotate(UnityEngine.Vector3 rotationVector) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, UnityEngine.Space space) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, space, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, float duration) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, duration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, currentProgram.defaultDuration, easing));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, UnityEngine.Space space, float duration) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, space, duration, currentProgram.defaultEase));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, UnityEngine.Space space, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, space, currentProgram.defaultDuration, easing));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, float duration, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, currentProgram.defaultSpace, duration, easing));
	}
	
	public static void Rotate(UnityEngine.Vector3 rotationVector, UnityEngine.Space space, float duration, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.RotateVector, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, rotationVector, UnityEngine.Vector3.one, null, space, duration, easing));
	}
	
	#endregion
	#region Scale
	/// <summary>
	/// Scale this program's target Transform to the specified local scale. You may optionally specify a Duration and Ease type.
	/// </summary>
	public static void Scale(UnityEngine.Vector3 toScale) {
		currentProgram.instructions.Add (new Instruction(Operation.Scale, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, toScale, null, currentProgram.defaultSpace, currentProgram.defaultDuration, currentProgram.defaultEase));
	}
	
	public static void Scale(UnityEngine.Vector3 toScale, float duration, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.Scale, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, toScale, null, currentProgram.defaultSpace, duration, easing));
	}
	
	public static void Scale(UnityEngine.Vector3 toScale, float duration) {
		currentProgram.instructions.Add (new Instruction(Operation.Scale, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, toScale, null, currentProgram.defaultSpace, duration, currentProgram.defaultEase));
	}
	
	public static void Scale(UnityEngine.Vector3 toScale, Ease easing) {
		currentProgram.instructions.Add (new Instruction(Operation.Scale, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, toScale, null, currentProgram.defaultSpace, currentProgram.defaultDuration, easing));
	}	
	#endregion
	#region Fade
	public static void Fade(UnityEngine.Color toColor, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		Fade(defaultColorPropertyName, toColor, currentProgram.defaultDuration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void Fade(UnityEngine.Color toColor, float duration, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		Fade(defaultColorPropertyName, toColor, duration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void Fade(UnityEngine.Color toColor, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		Fade(defaultColorPropertyName, toColor, currentProgram.defaultDuration, easing, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void Fade(UnityEngine.Color toColor, float duration, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		Fade(defaultColorPropertyName, toColor, duration, easing, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void Fade(string colorName, UnityEngine.Color toColor, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		Fade(colorName, toColor, currentProgram.defaultDuration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void Fade(string colorName, UnityEngine.Color toColor, float duration, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		Fade(colorName, toColor, duration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void Fade(string colorName, UnityEngine.Color toColor, float duration, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		UnityEngine.Renderer[] renderers;
		int colorID = UnityEngine.Shader.PropertyToID(colorName);
		
		if (includeChildren) {
			renderers = currentProgram.target.GetComponentsInChildren<UnityEngine.Renderer>(true);
		} else {
			renderers = currentProgram.target.GetComponents<UnityEngine.Renderer>();
		}
		
		currentProgram.instructions.Add(Instruction.Fade(renderers, colorID, toColor, includeChildren, areRendererMaterialsVolatile, duration, easing));
	}
	
	#endregion
	#region FadeAlpha
	/// <summary>
	/// Fade the alpha of main color "_Color" in the materials of this program's target Transform's attached renderers. You may optionally specify a Duration and Ease type.
	/// </summary>
	public static void FadeAlpha(float toAlpha, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(defaultColorPropertyName, toAlpha, currentProgram.defaultDuration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void FadeAlpha(float toAlpha, float duration, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(defaultColorPropertyName, toAlpha, duration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void FadeAlpha(float toAlpha, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(defaultColorPropertyName, toAlpha, currentProgram.defaultDuration, easing, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void FadeAlpha(float toAlpha, float duration, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(defaultColorPropertyName, toAlpha, duration, easing, includeChildren, areRendererMaterialsVolatile);
	}
	
	/// <summary>
	/// Fade the alpha of a named color in the materials of this program's target Transform's attached renderers. You may optionally specify a Duration and Ease type.
	/// </summary>
	public static void FadeAlpha(string colorName, float toAlpha, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(colorName, toAlpha, currentProgram.defaultDuration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void FadeAlpha(string colorName, float toAlpha, float duration, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(colorName, toAlpha, duration, currentProgram.defaultEase, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void FadeAlpha(string colorName, float toAlpha, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		FadeAlpha(colorName, toAlpha, currentProgram.defaultDuration, easing, includeChildren, areRendererMaterialsVolatile);
	}
	
	public static void FadeAlpha(string colorName, float toAlpha, float duration, Ease easing, bool includeChildren = true, bool areRendererMaterialsVolatile = true) {
		UnityEngine.Renderer[] renderers;
		int colorID = UnityEngine.Shader.PropertyToID(colorName);
		
		if (includeChildren) {
			renderers = currentProgram.target.GetComponentsInChildren<UnityEngine.Renderer>(true);
		} else {
			renderers = currentProgram.target.GetComponents<UnityEngine.Renderer>();
		}
		
		currentProgram.instructions.Add(Instruction.FadeAlpha(renderers, colorID, toAlpha, includeChildren, areRendererMaterialsVolatile, duration, easing));
	}
	
	#endregion
	#region SetEnabled
	public static void SetEnabled(UnityEngine.Behaviour behaviour, bool enabled) {
		currentProgram.instructions.Add (Instruction.SetEnabled(behaviour, enabled));
	}
	
	public static void Enable(UnityEngine.Behaviour behaviour) {
		currentProgram.instructions.Add (Instruction.SetEnabled(behaviour, true));
	}
	
	public static void Disable(UnityEngine.Behaviour behaviour) {
		currentProgram.instructions.Add (Instruction.SetEnabled(behaviour, false));
	}	
	
	
	public static void SetEnabled(UnityEngine.Collider collider, bool enabled) {
		currentProgram.instructions.Add (Instruction.SetEnabled(collider, enabled));
	}
	
	public static void Enable(UnityEngine.Collider collider) {
		currentProgram.instructions.Add (Instruction.SetEnabled(collider, true));
	}
	
	public static void Disable(UnityEngine.Collider collider) {
		currentProgram.instructions.Add (Instruction.SetEnabled(collider, false));
	}	
	
	
	public static void SetEnabled(UnityEngine.Renderer renderer, bool enabled) {
		currentProgram.instructions.Add (Instruction.SetEnabled(renderer, enabled));
	}
	
	public static void Enable(UnityEngine.Renderer renderer) {
		currentProgram.instructions.Add (Instruction.SetEnabled(renderer, true));
	}
	
	public static void Disable(UnityEngine.Renderer renderer) {
		currentProgram.instructions.Add (Instruction.SetEnabled(renderer, false));
	}	
	
	
	public static void SetEnabled(UnityEngine.GameObject gameObject, bool enabled) {
		currentProgram.instructions.Add (Instruction.SetEnabled(gameObject, enabled));
	}
	
	public static void Enable(UnityEngine.GameObject gameObject) {
		currentProgram.instructions.Add (Instruction.SetEnabled(gameObject, true));
	}
	
	public static void Disable(UnityEngine.GameObject gameObject) {
		currentProgram.instructions.Add (Instruction.SetEnabled(gameObject, false));
	}	
	
	
	#endregion
	#region Call
	public static void Call(System.Action action) {
		currentProgram.instructions.Add (Instruction.Call(action));
	}
	
	#endregion
	/// <summary>
	/// Begin executing all commands in Tween's static buffer, clear the program list, and return a Job instance. You may specify whether to ignore Time.timeScale (to animate during game pauses), a job name, and delegates for when the job finishes or gets cancelled. The job is passed as a parameter to the delegates.
	/// </summary>
	public static Job Run(bool ignoreTimeScale = false, string jobName = "", Action<Job> onFinished = null, Action<Job> onCancelled = null) {
		currentProgram = null;
		
		Job job = new Job(programList, ignoreTimeScale, jobName, onFinished, onCancelled);
		
		programList.Clear();
		
		return job;
	}
	
	public sealed class Job {
		public UnityEngine.Coroutine Coroutine {get; private set;}
		public bool IgnoreTimeScale {get; set;}
		public string Name {get; set;}
		public Action<Job> OnFinished {get; set;}
		public Action<Job> OnCancelled {get; set;}
		
		public bool IsRunning {get; private set;}
		public bool IsFinished {get; private set;}
		public bool IsPaused {get; set;}
		public bool DidCancel {get; private set;}
		
		public Job(List<Program> programList, bool ignoreTimeScale, string name, Action<Job> onFinished, Action<Job> onCancelled) {
			List<UnityEngine.Coroutine> coroutineList = new List<UnityEngine.Coroutine>();
			
			this.IgnoreTimeScale = ignoreTimeScale;
			this.Name = name;
			this.OnFinished = onFinished;
			this.OnCancelled = onCancelled;
			this.IsRunning = true;
			
			if (ignoreTimeScale) {
				instance.RequestRealDeltaTime(this);
			}
			
			foreach (Program program in programList) {
				coroutineList.Add(instance.StartCoroutine(RunProgramCoroutine(program)));
			}
			
			this.Coroutine = instance.StartCoroutine(WaitForJobCompletion(coroutineList));
		}
		
		public void Cancel() {
			IsRunning = false;
			IsFinished = true;
			DidCancel = true;
		}
		
		private IEnumerator RunProgramCoroutine(Program program) {
			List<UnityEngine.Coroutine> coroutineList = new List<UnityEngine.Coroutine>();
			List<Instruction> instructions = program.instructions;
			UnityEngine.Transform target = program.target;
			IEnumerator e;
			
			while (instructions.Count > 0 && !DidCancel) {
				Instruction instruction = instructions[0];
				
				if ((instruction.Opcode & Operation.Parent) != Operation.None) {
					target.parent = instruction.TransformValue;
					instructions.RemoveAt(0);
					continue;
				}
				
				if ((instruction.Opcode & (Operation.Delay | Operation.Flush)) != Operation.None) {
					e = WaitCoroutine(instruction.Duration);
					while (e.MoveNext()) {
						yield return e.Current;
					}
					instructions.RemoveAt(0);
					continue;
				}
				
				if ((instruction.Opcode & Operation.SetEnabled) != Operation.None) {
					if (instruction.Behaviour != null) {
						instruction.Behaviour.enabled = instruction.Enabled;
					}
					if (instruction.Collider != null) {
						instruction.Collider.enabled = instruction.Enabled;
					}
					if (instruction.Renderer != null) {
						instruction.Renderer.enabled = instruction.Enabled;
					}
					if (instruction.GameObject != null) {
						instruction.GameObject.SetActive(instruction.Enabled);
					}
					instructions.RemoveAt(0);
					continue;
				}
				
				if ((instruction.Opcode & Operation.Call) != Operation.None) {
					instruction.Action();
					instructions.RemoveAt(0);
					continue;
				}
				
				coroutineList.Add(instance.StartCoroutine(InterruptableInstructionCoroutine(target, instruction)));
				
				instructions.RemoveAt(0);
			}
			
			e = WaitForCoroutineCompletion(coroutineList);
			while (e.MoveNext()) {
				yield return e.Current;
			}
		}
		
		IEnumerator WaitCoroutine(float duration) {
			float timer = 0f;
			
			while (timer < duration && !DidCancel && !IsFinished) {				
				if (!IsPaused) {
					timer += IgnoreTimeScale ? instance.RealDeltaTime : UnityEngine.Time.deltaTime;
				}
					
				if (timer > duration) {
					timer = duration;
				}
				
				yield return null;
			}			
		}
		
		IEnumerator WaitForJobCompletion(List<UnityEngine.Coroutine> coroutineList) {
			foreach (UnityEngine.Coroutine coroutine in coroutineList) {
				yield return coroutine;
			}
			
			if (DidCancel) {
				if (OnCancelled != null) {
					OnCancelled(this);
				}
				yield break;
			}
			
			IsRunning = false;
			IsFinished = true;
			if (OnFinished != null) {
				OnFinished(this);
			}
		}
		
		IEnumerator WaitForCoroutineCompletion(List<UnityEngine.Coroutine> coroutineList) {
			foreach (UnityEngine.Coroutine coroutine in coroutineList) {
				if (DidCancel || IsFinished) {
					yield break;
				}
				yield return coroutine;
			}
		}
		
		// Move into Instruction class
		IEnumerator InterruptableInstructionCoroutine(UnityEngine.Transform target, Instruction instruction) {
			UnityEngine.Vector3 fromPosition = UnityEngine.Vector3.zero;
			UnityEngine.Quaternion fromRotation = UnityEngine.Quaternion.identity;
			UnityEngine.Vector3 fromScale = UnityEngine.Vector3.one;
			
			UnityEngine.Material[] materials = instruction.Materials;
			UnityEngine.Color[] fromColorArray = null;

			if ((instruction.Opcode & Operation.Move) != Operation.None) {
				if ((instruction.Opcode & Operation.MoveRelativeTo) != Operation.None) {
					fromPosition = target.position;
				} else {
					fromPosition = instruction.Space == UnityEngine.Space.Self ? target.localPosition : target.position;
				}
			}
			
			if ((instruction.Opcode & Operation.Rotate) != Operation.None) {
				fromRotation = instruction.Space == UnityEngine.Space.Self ? target.localRotation : target.rotation;
			}
			
			if ((instruction.Opcode & Operation.Scale) != Operation.None) {
				fromScale = target.localScale;
			}
			
			if ((instruction.Opcode & (Operation.FadeAlpha | Operation.Fade)) != Operation.None) {
				
				fromColorArray = new UnityEngine.Color[materials.Length];
				
				for (int i=0; i < materials.Length; i++) {
					if (!materials[i].HasProperty(instruction.ColorPropertyID)) {
						fromColorArray[i] = UnityEngine.Color.white;
					} else {
						fromColorArray[i] = materials[i].GetColor(instruction.ColorPropertyID);
					}
				}
			}

			float timer = 0f;
			float deltaTime = 0f;
			
			while (timer <= instruction.Duration && !DidCancel && !IsFinished) {
				deltaTime = IsPaused ? 0f : (IgnoreTimeScale ? instance.RealDeltaTime : UnityEngine.Time.deltaTime);

				timer += deltaTime;
							
				if (timer > instruction.Duration) {
					timer = instruction.Duration;
				}
				
				float factorComplete = 1f;
				
				if (instruction.Duration > 0f) {
					factorComplete = timer / instruction.Duration;
				}
				
				float interpolatedFactorComplete = instruction.EaseValue.Interpolate(factorComplete);
				
				if (instruction.Space == UnityEngine.Space.Self) {
					if ((instruction.Opcode & Operation.Move) != Operation.None) {
						target.localPosition = Lerp(fromPosition, instruction.Position, interpolatedFactorComplete);
					}
					
					if ((instruction.Opcode & Operation.Rotate) != Operation.None) {
						target.localRotation = Lerp(fromRotation, instruction.Rotation, interpolatedFactorComplete);
					} else if ((instruction.Opcode & Operation.RotateVector) != Operation.None) {
						target.localRotation = fromRotation
							* UnityEngine.Quaternion.AngleAxis(interpolatedFactorComplete * instruction.RotationVector.x, UnityEngine.Vector3.Scale(instruction.RotationVector, (instruction.RotationVector.x >= 0f ? 1f : -1f) * UnityEngine.Vector3.right))
								* UnityEngine.Quaternion.AngleAxis(interpolatedFactorComplete * instruction.RotationVector.y, UnityEngine.Vector3.Scale(instruction.RotationVector, (instruction.RotationVector.y >= 0f ? 1f : -1f) * UnityEngine.Vector3.up))
								* UnityEngine.Quaternion.AngleAxis(interpolatedFactorComplete * instruction.RotationVector.z, UnityEngine.Vector3.Scale(instruction.RotationVector, (instruction.RotationVector.z >= 0f ? 1f : -1f) * UnityEngine.Vector3.forward));
					}
					
					if ((instruction.Opcode & Operation.MoveRelativeTo) != Operation.None) {
						target.position = Lerp(fromPosition, instruction.TransformValue.TransformDirection(instruction.Position) + instruction.TransformValue.position, interpolatedFactorComplete);
					}
				} else {
					if ((instruction.Opcode & Operation.Move) != Operation.None) {
						target.position = Lerp(fromPosition, instruction.Position, interpolatedFactorComplete);
					}
					
					if ((instruction.Opcode & Operation.Rotate) != Operation.None) {
						target.rotation = Lerp(fromRotation, instruction.Rotation, interpolatedFactorComplete);
					} else if ((instruction.Opcode & Operation.RotateVector) != Operation.None) {
						target.rotation = fromRotation
							* UnityEngine.Quaternion.AngleAxis(interpolatedFactorComplete * instruction.RotationVector.x, UnityEngine.Vector3.Scale(instruction.RotationVector, (instruction.RotationVector.x >= 0f ? 1f : -1f) * UnityEngine.Vector3.right))
								* UnityEngine.Quaternion.AngleAxis(interpolatedFactorComplete * instruction.RotationVector.y, UnityEngine.Vector3.Scale(instruction.RotationVector, (instruction.RotationVector.y >= 0f ? 1f : -1f) * UnityEngine.Vector3.up))
								* UnityEngine.Quaternion.AngleAxis(interpolatedFactorComplete * instruction.RotationVector.z, UnityEngine.Vector3.Scale(instruction.RotationVector, (instruction.RotationVector.z >= 0f ? 1f : -1f) * UnityEngine.Vector3.forward));
					}
					
					if ((instruction.Opcode & Operation.MoveRelativeTo) != Operation.None) {
						target.position = Lerp(fromPosition, instruction.TransformValue.position + instruction.Position, interpolatedFactorComplete);
					}
				}
				
				if ((instruction.Opcode & Operation.Scale) != Operation.None) {
					target.localScale = Lerp(fromScale, instruction.Scale, interpolatedFactorComplete);
				}
				
				if ((instruction.Opcode & Operation.Custom) != Operation.None) {
					instruction.CustomInstruction(interpolatedFactorComplete);
				}
				
				if ((instruction.Opcode & (Operation.Fade | Operation.FadeAlpha)) != Operation.None) {
					
					if (instruction.isVolatile) {
						UnityEngine.Renderer[] renderers = instruction.Renderers;
						bool needsUpdate = false;
						
						for (int i=0; i < renderers.Length; i++) {
							UnityEngine.Material[] rendererMaterials = renderers[i].materials;
							
							for (int materialIndex=0; materialIndex < rendererMaterials.Length; materialIndex++) {
								bool found = false;
								
								for (int j=0; j < materials.Length; j++) {
									if (materials[j] == rendererMaterials[materialIndex]) {
										found = true;
										break;
									}
								}
								
								if (!found) {
									needsUpdate = true;
									goto UpdateMaterials;
								}
							}
						}
						
					UpdateMaterials:
						if (needsUpdate) {
							List<UnityEngine.Material> materialList = new List<UnityEngine.Material>();
							
							for (int i=0; i < renderers.Length; i++) {
								UnityEngine.Material[] rendererMaterials = renderers[i].materials;
								
								for (int materialIndex=0; materialIndex < rendererMaterials.Length; materialIndex++) {
									materialList.Add(rendererMaterials[materialIndex]);
								}
							}
							
							UnityEngine.Color[] newFromColorArray = new UnityEngine.Color[materialList.Count];
							
							for (int i=0; i < materialList.Count; i++) {
								int index = System.Array.IndexOf(materials, materialList[i]);
								
								// Either copy over the existing fromColorArray element or get the color because it is new
								// This is because chances are the current color of the materials we already had is 99.9% probably changed by Tween and we don't want it.
								if (index != -1) {
									newFromColorArray[i] = fromColorArray[index];
								} else {
									newFromColorArray[i] = materialList[i].GetColor(instruction.ColorPropertyID);
								}
							}
							
							materials = materialList.ToArray();
							fromColorArray = newFromColorArray;
						}
					}
					
					for (int i=0; i<materials.Length; i++) {
						UnityEngine.Color color = fromColorArray[i];
						UnityEngine.Color newColor;
						
						if ((instruction.Opcode & Operation.FadeAlpha) != Operation.None) {
							newColor = color;
							newColor.a = UnityEngine.Mathf.Lerp(color.a, instruction.Color.a, interpolatedFactorComplete); // Use builtin lerps so we get clamping
						} else {
							newColor = UnityEngine.Color.Lerp(color, instruction.Color, interpolatedFactorComplete); // Use builtin lerps so we get clamping
						}
						
						materials[i].SetColor(instruction.ColorPropertyID, newColor);
//						UnityEngine.Debug.Log(string.Format("Time:{3}    From:{0} To:{1} Factor:{2}", color.a.ToString("f3"), instruction.Color.a.ToString ("f3"), interpolatedFactorComplete.ToString("f3"), UnityEngine.Time.time.ToString("f3")));
					}
				}
				
				if (timer >= instruction.Duration) { // This will let zero-length instructions run, because that could be useful.
					yield break;
				}
				
				yield return null;
			}
		}
		
		// This Lerp doesn't clamp like UnityEngine.Vector3.Lerp. That's why it's here.
		static UnityEngine.Vector3 Lerp(UnityEngine.Vector3 from, UnityEngine.Vector3 to, float t) {
			return new UnityEngine.Vector3(((1f - t) * from.x) + (t * to.x),
			                               ((1f - t) * from.y) + (t * to.y),
			                               ((1f - t) * from.z) + (t * to.z));
		}
		
		// This Lerp doesn't clamp like UnityEngine.Quaternion.Lerp. That's why it's here.
		static UnityEngine.Quaternion Lerp(UnityEngine.Quaternion from, UnityEngine.Quaternion to, float t) {
			return new UnityEngine.Quaternion(((1f - t) * from.x) + (t * to.x),
			                                  ((1f - t) * from.y) + (t * to.y),
			                                  ((1f - t) * from.z) + (t * to.z),
			                                  ((1f - t) * from.w) + (t * to.w));
		}
		
		// This Lerp doesn't clamp like UnityEngine.Color.Lerp. That's why it's here.
		static UnityEngine.Color Lerp(UnityEngine.Color from, UnityEngine.Color to, float t) {
			return new UnityEngine.Color(((1f - t) * from.r) + (t * to.r),
			                             ((1f - t) * from.g) + (t * to.g),
			                             ((1f - t) * from.b) + (t * to.b),
			                             ((1f - t) * from.a) + (t * to.a));
		}
		
		
		// This Lerp doesn't clamp like UnityEngine.Mathf.Lerp. That's why it's here.
		static float Lerp(float from, float to, float t) {
			return (1f - t) * from + t * to;
		}
	}	
#endregion
#region Internal Implementation
	private const string defaultColorPropertyName = "_Color";
	
	static Tween instance;
	static Program currentProgram;
	static List<Program> programList = new List<Program>();
	
	// Make it a bitmask because maybe I'll combine together instructions later so less coroutines running
	public enum Operation {
		None				= 0,
		Delay				= 1,
		Flush				= 2,
		Parent				= 4,
		Move				= 8,
		Rotate				= 16,
		Scale				= 32,
		MoveRelativeTo		= 64,
		RotateVector		= 128,
		Fade				= 256,
		FadeAlpha			= 512,
		SetEnabled			= 1024,
		Custom				= 2048,
		Call				= 4096,
	}
	
	public class Instruction {
		public Operation Opcode {get; private set;}
		public UnityEngine.Vector3 Position {get; private set;}
		public UnityEngine.Vector3 Scale {get; private set;}
		public UnityEngine.Quaternion Rotation {get; private set;}
		public UnityEngine.Vector3 RotationVector {get; private set;}
		public UnityEngine.Transform TransformValue {get; private set;}
		public UnityEngine.Space Space {get; private set;}
		
		public UnityEngine.Material[] Materials {get; set;}
		public UnityEngine.Renderer[] Renderers {get; set;}
		public bool IncludeChildren {get; private set;}
		public bool isVolatile {get; private set;}
		public int ColorPropertyID {get; private set;}
		public UnityEngine.Color Color {get; private set;}
		
		public UnityEngine.Behaviour Behaviour {get; private set;}
		public UnityEngine.Collider Collider {get; private set;}
		public UnityEngine.GameObject GameObject {get; private set;}
		public UnityEngine.Renderer Renderer {get; private set;}
		public bool Enabled {get; private set;}
		
		public float Duration {get; private set;}
		public Ease EaseValue {get; private set;}
		
		public System.Action<float> CustomInstruction {get; private set;}
		public System.Action Action {get; private set;}

		public object AdditionalData {get; private set;}
		
		private Instruction(Operation operation) {
			this.Opcode = operation;
		}
		
		private Instruction(Operation operation, float duration, Ease easeValue) {
			this.Opcode = operation;
			this.Duration = duration;
			this.EaseValue = easeValue;
		}
		
		public Instruction(Operation operation, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Vector3 rotationVector, UnityEngine.Vector3 scale, UnityEngine.Transform transformValue, UnityEngine.Space space, float duration, Ease easeValue) {
			this.Opcode = operation;
			this.Position = position;
			this.Rotation = rotation;
			this.RotationVector = rotationVector;
			this.Scale = scale;
			this.TransformValue = transformValue;
			this.Space = space;
			this.Duration = duration;
			this.EaseValue = easeValue;
		}

		public Instruction(Operation operation, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Vector3 rotationVector, UnityEngine.Vector3 scale, UnityEngine.Transform transformValue, UnityEngine.Space space, float duration, Ease easeValue, object additionalData) {
			this.Opcode = operation;
			this.Position = position;
			this.Rotation = rotation;
			this.RotationVector = rotationVector;
			this.Scale = scale;
			this.TransformValue = transformValue;
			this.Space = space;
			this.Duration = duration;
			this.EaseValue = easeValue;
			this.AdditionalData = additionalData;
		}
			
		public static Instruction Delay (float duration) {
			return new Instruction(Operation.Delay, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, null, UnityEngine.Space.Self, duration, Ease.Default);
		}
		
		public static Instruction Flush (float duration) {
			return new Instruction(Operation.Flush, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, null, UnityEngine.Space.Self, duration, Ease.Default);
		}
		
		public static Instruction Custom (System.Action<float> customInstruction, float duration, Ease easeValue) {
			Instruction instruction = new Instruction(Operation.Custom, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, null, UnityEngine.Space.Self, duration, easeValue);
			instruction.CustomInstruction = customInstruction;
			return instruction;
		}
		
		public static Instruction Parent(UnityEngine.Transform transformValue) {
			return new Instruction(Operation.Parent, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, transformValue, UnityEngine.Space.Self, 0f, Ease.Default);
		}
		
		public static Instruction SetEnabled(UnityEngine.Behaviour behaviour, bool enabled) {
			Instruction instruction = new Instruction(Operation.SetEnabled);
			instruction.Behaviour = behaviour;
			instruction.Enabled = enabled;
			return instruction;
		}
		
		public static Instruction SetEnabled(UnityEngine.Collider collider, bool enabled) {
			Instruction instruction = new Instruction(Operation.SetEnabled);
			instruction.Collider = collider;
			instruction.Enabled = enabled;
			return instruction;
		}
		
		public static Instruction SetEnabled(UnityEngine.Renderer renderer, bool enabled) {
			Instruction instruction = new Instruction(Operation.SetEnabled);
			instruction.Renderer = renderer;
			instruction.Enabled = enabled;
			return instruction;
		}
		
		public static Instruction SetEnabled(UnityEngine.GameObject gameObject, bool enabled) {
			Instruction instruction = new Instruction(Operation.SetEnabled);
			instruction.GameObject = gameObject;
			instruction.Enabled = enabled;
			return instruction;
		}
		
		public static Instruction Fade(UnityEngine.Renderer[] renderers, int colorPropertyID, UnityEngine.Color color, bool includeChildren, bool areRendererMaterialsVolatile, float duration, Ease easeValue) {
			Instruction instruction = new Instruction(Operation.Fade, duration, easeValue);
			instruction.ColorPropertyID = colorPropertyID;
			instruction.Color = color;
			instruction.IncludeChildren = includeChildren;
			instruction.isVolatile = areRendererMaterialsVolatile;
			instruction.Renderers = renderers;
			
			List<UnityEngine.Material> materialList = new List<UnityEngine.Material>();
			
			foreach (UnityEngine.Renderer renderer in renderers) {
				foreach (UnityEngine.Material material in renderer.materials) {
					materialList.Add(material);
				}
			}
			
			instruction.Materials = materialList.ToArray();
			
			return instruction;
		}
		
		public static Instruction FadeAlpha(UnityEngine.Renderer[] renderers, int colorPropertyID, float alpha, bool includeChildren, bool areRendererMaterialsVolatile, float duration, Ease easeValue) {
			Instruction instruction = new Instruction(Operation.FadeAlpha, duration, easeValue);
			instruction.ColorPropertyID = colorPropertyID;
			instruction.Color = new UnityEngine.Color(0f,0f,0f,alpha);
			instruction.IncludeChildren = includeChildren;
			instruction.isVolatile = areRendererMaterialsVolatile;
			instruction.Renderers = renderers;
			
			List<UnityEngine.Material> materialList = new List<UnityEngine.Material>();
			
			foreach (UnityEngine.Renderer renderer in renderers) {
				foreach (UnityEngine.Material material in renderer.materials) {
					materialList.Add(material);
				}
			}
			
			instruction.Materials = materialList.ToArray();
			
			return instruction;
		}
		
		public static Instruction Call(System.Action action) {
			Instruction instruction = new Instruction(Operation.Call);
			instruction.Action = action;
			return instruction;
		}
	}
	
	public class Program {
		public UnityEngine.Transform target;
		public List<Instruction> instructions = new List<Instruction>();
		
		public float defaultDuration = 0.33333f;
		public Ease defaultEase = Ease.Default;
		public UnityEngine.Space defaultSpace = UnityEngine.Space.Self;
		public UnityEngine.Color defaultColor = UnityEngine.Color.white;
		
		public Program(UnityEngine.Transform target) {
			this.target = target;
		}
	}
	
	public float RealDeltaTime {get; private set;}
	
	List<Job> jobRequestingRealDeltaTimeList = new List<Job>();
	DateTime lastDateTime;
	
	void RequestRealDeltaTime(Job job) {
		jobRequestingRealDeltaTimeList.Add(job);
		
		if (jobRequestingRealDeltaTimeList.Count < 2) {
			lastDateTime = DateTime.Now;
			StartCoroutine(ComputeRealDeltaTime());
		}
	}
	
	IEnumerator ComputeRealDeltaTime() {
		while (jobRequestingRealDeltaTimeList.Count > 0) {
			DateTime now = DateTime.Now;
			RealDeltaTime = (float)((now - lastDateTime).TotalSeconds);
			lastDateTime = now;
			
			for (int i=0; i < jobRequestingRealDeltaTimeList.Count; i++) {
				if (!jobRequestingRealDeltaTimeList[i].IsRunning) {
					jobRequestingRealDeltaTimeList.RemoveAt (i);
					i--;
				}
			}
			
			yield return null;
		}
	}
	
#endregion
}
