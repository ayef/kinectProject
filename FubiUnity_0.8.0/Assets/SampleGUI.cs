using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
	
public class SampleGUI : MonoBehaviour
{
	public GUISkin guiskin;
	
	bool buttonPressed = false;
	bool gesturePerformed = false;
	string swipeMenuSelection = "";
	string[] options = {"option1", "option2", "option3", "option4"};
		
	void Start ()
	{		
	}

	void Update ()
	{

	}

	void OnGUI ()
	{
		GUI.skin = guiskin;
		
		float screenH8th = Screen.height / 8.0f;
		float screenH4th = Screen.height / 4.0f;
		float screenHHalf = Screen.height / 2.0f;
		float screenW8th = Screen.width / 8.0f;
		float screenW4th = Screen.width / 4.0f;
		float screenWHalf = Screen.width / 2.0f;			
			
		if (FubiUnity.FubiButton (new Rect (screenW8th, screenH8th, screenW8th, 100), "Fubi Button", "FubiButton"))
		{
			buttonPressed = true;
		}
		
		// AA: commented fubi swipe menu
//		string selection = FubiUnity.FubiSwipeMenu(new Vector2(screenWHalf, screenHHalf), screenH4th, options, "FubiSwipeMenu", "FubiSwipeCenter");
//		if (selection.Length > 0)
//			swipeMenuSelection = selection;

		// AA: commented fubi gesture animation
		if (FubiUnity.FubiGesture (new Rect (screenWHalf + screenW8th, screenH8th, screenW8th, screenH4th), "RightHandWavingAboveTheShoulder", new GUIStyle()))
		{
			gesturePerformed = true;
		}
		
// AA: No messages required		
//		if (buttonPressed)
//			GUI.Box(new Rect (screenW8th, screenHHalf + screenH4th, screenW4th, 100), "Button pressed!");
//		
//		if (swipeMenuSelection.Length > 0)
//			GUI.Box(new Rect (screenWHalf - screenW8th, screenHHalf + screenH4th, screenW4th, 100), "Last Selection: "+swipeMenuSelection);
//		
//		if (gesturePerformed)
//			GUI.Box (new Rect (screenWHalf + screenW8th, screenHHalf + screenH4th, screenW4th, 100), "Waving gesture performed!");
	}
}