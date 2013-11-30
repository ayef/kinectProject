using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AA: Manages all the added filters
public class FilterManager {

	public List<Filter> filters;	// List of added filters
	public List<Vector3> joints;	// List of smoothed joint results
	public List<Vector3> relativeJoints;	// List of smoothed relative joint results
	public List<Vector2> vectors;	// List of smoothed vectors
	public List<Vector2> relativeCursorPosition;	// Store the previous relative cursor position for each filter
	public List<Vector2> prevAbsPixelPosition;	// Store the previous absolute cursor position for each filter
	public List<Vector2> absPixelPosition;	// Store the current absolute cursor position for each filter
	public List<Color> colors;	// Store the color for this filter absolute cursor position for each filter
	public int numColors = 13;

	Color[] listOfColors;

	
	public FilterManager()
	{
		filters = new List<Filter> ();
		joints = new List<Vector3>();
		relativeJoints = new List<Vector3>();
		vectors = new List<Vector2>();
		relativeCursorPosition = new List<Vector2>();
		prevAbsPixelPosition = new List<Vector2>();
		absPixelPosition =  new List<Vector2>();
		colors = new List<Color>();
		
		listOfColors = new Color[numColors];
		listOfColors[0] = Color.red;
		listOfColors[1] = Color.blue;
		listOfColors[2] = Color.yellow;
		listOfColors[3] = Color.black;
		listOfColors[4] = Color.cyan;
		listOfColors[5] = Color.green;
		listOfColors[6] = Color.black;
		listOfColors[7] = Color.cyan;
		listOfColors[8] = Color.green;
		listOfColors[9] = Color.yellow;
		listOfColors[10] = Color.blue;
		listOfColors[11] = Color.red;
		listOfColors[12] = Color.green;
		listOfColors[12] = Color.cyan;

		for (int i = 0; i < numColors; i++) {
			Debug.Log("colors: " + listOfColors[i].ToString());
		}

	}	

	// Adds an initialised filter to the list of filters 
	public void AddFilter(Filter filter)	
	{
		filters.Add(filter);
		relativeCursorPosition.Add (new Vector2(0,0));
		prevAbsPixelPosition.Add (new Vector2(0,0));
		absPixelPosition.Add (new Vector2(0,0));
		colors.Add(listOfColors[filters.Count-1]);
	}
	
	// Pases all possible values that all the filters could need and calls Update function for each filter
	public void UpdateJointFilters(Vector3 joint,  Vector3 relativeJoint) 
	{
		Vector3 jointRes = new Vector3(0,0,0);
		Vector3 relJointRes = new Vector3(0,0,0);
		joints.Clear();
		relativeJoints.Clear ();
		
		for (int i = 0; i < filters.Count; i++) {
			jointRes = filters[i].Update (joint, Filter.JOINT_TYPE.JOINT);
			relJointRes = filters[i].Update (relativeJoint, Filter.JOINT_TYPE.RELATIVEJOINT);
			joints.Add (jointRes);
			relativeJoints.Add ( relJointRes);
			//Debug.Log ("joints " + i +" " + joints[i] + " relJoint " + relativeJoints[i] + filters[i].name.ToString());
		}
		
	}

	// Pases all possible values that all the filters could need and calls Update function for each filter
	public void UpdateVectorFilters(Vector2 prevScreenPos, Vector2 newScreenPos, float filterFactor) 
	{
		Vector2 vectorRes = new Vector2(0,0);
		vectors.Clear ();
		for (int i = 0; i < filters.Count; i++) {
			vectorRes = filters[i].Update (prevScreenPos , newScreenPos, filterFactor);
			vectors.Add(vectorRes);
		}
	}
	
	// Empties list of filters
	public void Clear() 
	{
		filters.Clear ();
		relativeCursorPosition.Clear();
		prevAbsPixelPosition.Clear();
		absPixelPosition.Clear();
		colors.Clear ();
		
	}
}
