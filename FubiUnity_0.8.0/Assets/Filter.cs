using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Filter {
// Author: AA
// Contains all filters and logging of filtered points
	
	Vector2 [] jointsVectorHistory;		// To keep history of vector between joint and relative joint
	Vector3 [] jointHistory;			// To keep history of hand positions 
	Vector3 [] relativeJointHistory;	// To keep history of relative joint, could be shoulder or elbow number of previous values of 
	float [] weights;
	
	Vector2 [] jointsVectorOutputs;		// To keep history of outputs of filter 
	Vector3 [] jointOutputs;			// "
	Vector3 [] relativeJointOutputs;	// "

	// In case we want to control which filters are used
	bool bUseWMA = true;
	bool bUseTaylorSeries = true;
	bool bUseKalman = true;
	
	public enum FILTER_NAME 
	{
		SIMPLE_AVG,
		MOVING_AVG,
		DOUBLE_MOVING_AVG,
		EXP_SMOOTHING,
		DOUBLE_EXP_SMOOTHING,
		ADAPTIVE_DOUBLE_EXP_SMOOTHING,
		TAYLOR_SERIES,
		MEDIAN,
		JITTER_REMOVAL,
		COMBINATION1,	// Predefined combinations of filters
		COMBINATION2,
		NONE
	};		
	public FILTER_NAME name;

	public enum JOINT_TYPE 
	{
		VECTOR,
		JOINT,
		RELATIVEJOINT
	};		
	
	public int numHistory = 10;			// Length of history to keep recommended value = 3, greater than this causes too much latency
	int jointIndex = 0;				// Indices into joint and relative joint histories
	int relativeJointIndex = 0;
	float highestWeight = 0.7f;
	// TODO: This class must be responsible for maintaining logs of joint filtered and unfiltered data for comparison
	 
	
	// Keep history of previous ten values
	/// <summary>
	/// Initializes a new instance of the <see cref="Filter"/> class.
	/// </summary>
	public Filter(){
		jointHistory = new Vector3 [numHistory];
		relativeJointHistory = new Vector3 [numHistory];
		jointsVectorHistory = new Vector2 [numHistory];
		weights = new float [numHistory];
		name = FILTER_NAME.MOVING_AVG;
//List<Vector2> myvec;
//		myvec.Sort ();
//		myvec.RemoveAt (myvec.Count)
		float tempWeight = highestWeight;
		float sumWeights = 0f;
		
		for (int i = 0; i < numHistory; i++) {
			jointHistory[i] = relativeJointHistory[i] = jointsVectorHistory[i] = Vector3.zero;
			
			if(i != numHistory-1) {
				weights[i] = tempWeight;
				sumWeights += weights[i];
				tempWeight = (1 - sumWeights)/2;
			}
			else {
				weights[i] = 1 - sumWeights;
			}
			//Debug.Log ("weight: " + i + " " + weights[i]);
		}
		
	}
	
	// AA: Interface function for filtering of individual joints
	public  Vector3 Update(Vector3 jointPos, JOINT_TYPE jointType ) {
		Vector3 newJointPos = Vector3.zero;

		switch (jointType) {
		
		case JOINT_TYPE.JOINT:
			// Loop arond the joint history buffers if necessary
			if(jointIndex > numHistory - 1) 
				jointIndex = jointIndex % numHistory;
			
			jointHistory[jointIndex] = jointPos;
			newJointPos = applyFilter(jointHistory, jointType);
			jointHistory[jointIndex] = newJointPos;
			jointIndex++;
			break;

		case JOINT_TYPE.RELATIVEJOINT:
			// Loop arond the joint history buffers if necessary
			if(relativeJointIndex > numHistory - 1) 
				relativeJointIndex = relativeJointIndex % numHistory;
			
			// Store joint in history
			relativeJointHistory[relativeJointIndex] = jointPos;
			newJointPos = applyFilter(relativeJointHistory, jointType);
			relativeJointHistory[relativeJointIndex] = newJointPos;
			relativeJointIndex++;
				
			break;
		default:
		break;

		}	
		
		return newJointPos;
	}

	private Vector3 applyFilter(Vector3 [] array, JOINT_TYPE jointType) {
		
		Vector3 sum = Vector3.zero;

		switch (name) {

		// Simplest joint filter, where the filter output is the average of N recent inputs
		case FILTER_NAME.SIMPLE_AVG:
			
			for (int i = 0; i < numHistory; i++) {
				sum = sum + array[i];
			}
			sum/=numHistory;
			break;

		// Moving average (MA) filters are a special case of ARMA filters where the auto regressive term is zero
		case FILTER_NAME.MOVING_AVG:
			
			for (int i = 0; i < numHistory; i++) {
				sum = sum + array[i]*weights[i];
			}
			break;
			
		default:
		break;
		}
	
		return sum;
	}
	// AA: Interface function for filtering vector
	//
	public Vector2 Update(Vector2 previousVector, Vector2 currentVector, float weightingFactor) {
		// Log the values in history
		
		// Compute new vector as weighted sum of new and previous vector
		return WMA_Filter(previousVector, currentVector, weightingFactor);
	}
	
	private Vector2 WMA_Filter(Vector2 relPos, Vector2 newPos, float filterFactor)
	{
		Vector2 temp = Vector2.zero;
		temp.x = (1.0f - filterFactor) * relPos.x + filterFactor * newPos.x;
		temp.y = (1.0f - filterFactor) * relPos.y + filterFactor * newPos.y;
		return temp;
	}	
}
