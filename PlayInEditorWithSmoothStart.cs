using UnityEngine;
using UnityEditor;
using System.Collections;

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
