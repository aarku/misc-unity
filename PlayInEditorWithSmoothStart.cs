using UnityEngine;
using UnityEditor;

// Helps avoid the annoying time jerk at the start when you play your game in the editor.
// The license is the "Unlicense." See LICENSE for details
// Enjoy! @jonathanczeck
public static class PlayInEditorWithSmoothStart {
	static float savedMaximumDeltaTime = 0f;
	
	[MenuItem("Edit/Play (Start Smoothly) %[")]
	static void PlayStartSmoothly() {
		if (EditorApplication.isPlaying) {
			EditorApplication.isPlaying = false;
		} else {
			EditorApplication.isPlaying = true;
			savedMaximumDeltaTime = Time.maximumDeltaTime;
			Time.maximumDeltaTime = 0f;
			EditorApplication.update += PlayStartSmoothlyHelper;
		}
	}
	
	static void PlayStartSmoothlyHelper() {
		if (Time.frameCount > 1) {
			if (Time.maximumDeltaTime == 0f) {
				Time.maximumDeltaTime = savedMaximumDeltaTime;
			}
			EditorApplication.update -= PlayStartSmoothlyHelper;
		}
	}
}
