using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Filter {
// Author: AA
// Contains all filters and logging of filtered points
	
	List<Vector3> jointsMedian;			// To keep sorted history for past inputs for MEDIAN filter
	List<Vector3> relativeJointsMedian;	// To keep sorted history for past inputs for MEDIAN filter
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
	int jointIndex = 0;					// Indices into joint and relative joint histories
	int relativeJointIndex = 0;
	int jointOutputIndex = 0;					// Indices into joint and relative joint Output histories
	int relativeJointOutputIndex = 0;
	public float highestWeight = 0.7f;	// Used by Weighted Moving average filter 
	public float alpha = 0.5f;			// Used by Exponential smoothing filter
	public float gamma = 0.5f;			// Used by Double Exponential smoothing filter
	public int windowSize = 2;		// Used by many filters, the amount of history to consider
	 
	
	// Keep history of previous ten values
	/// <summary>
	/// Initializes a new instance of the <see cref="Filter"/> class.
	/// </summary>
	public Filter(){
		jointHistory = new Vector3 [numHistory];
		relativeJointHistory = new Vector3 [numHistory];
		jointsVectorHistory = new Vector2 [numHistory];
		
		jointOutputs = new Vector3 [numHistory];
		relativeJointOutputs = new Vector3 [numHistory];
		jointsVectorOutputs= new Vector2 [numHistory];

		jointsMedian = new List<Vector2>();
		relativeJointsMedian = new List<Vector2>();

		weights = new float [numHistory];
		name = FILTER_NAME.MOVING_AVG;

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
			
			if(jointOutputIndex > numHistory - 1) 
				jointOutputIndex = jointOutputIndex % numHistory;
			
			
			jointHistory[jointIndex] = jointPos;
			newJointPos = applyFilter(jointHistory, jointType, jointOutputs);

			//jointHistory[jointIndex] = newJointPos;
			jointIndex++;
			
			jointOutputs[jointOutputIndex] = newJointPos;
			jointOutputIndex++;
			break;

		case JOINT_TYPE.RELATIVEJOINT:
			// Loop arond the joint history buffers if necessary
			if(relativeJointIndex > numHistory - 1) 
				relativeJointIndex = relativeJointIndex % numHistory;

			if(relativeJointOutputIndex > numHistory - 1) 
				relativeJointOutputIndex = relativeJointOutputIndex % numHistory;
			
			relativeJointsMedian.Add (jointPos);
			// Store joint in history
			relativeJointHistory[relativeJointIndex] = jointPos;
			newJointPos = applyFilter(relativeJointHistory, jointType, relativeJointOutputs);
			
			//relativeJointHistory[relativeJointIndex] = newJointPos;
			relativeJointIndex++;
			
			relativeJointOutputs[relativeJointOutputIndex] = newJointPos;
			relativeJointOutputIndex++;
			
			
				
			break;
		default:
		break;

		}	
		
		return newJointPos;
	}

	private Vector3 applyFilter(Vector3 [] array, JOINT_TYPE jointType, Vector3 [] arrayOutput = null) {
		
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
			
		// Double moving average with window size = 2 (for fast implementation)
		case FILTER_NAME.DOUBLE_MOVING_AVG:
			sum = (5.0f/9)*array[getIndex(jointIndex)] +
				  (4.0f/9)*array[getIndex(jointIndex-1)] +
					(1.0f/3)*array[getIndex(jointIndex-2)] -
					(2.0f/9)*array[getIndex(jointIndex-3)] -
					(1.0f/9)*array[getIndex(jointIndex-4)];
			break;
		// Double moving average with window size = 2 (for fast implementation)
		case FILTER_NAME.EXP_SMOOTHING:
//			for (int i = 0; i < windowSize; i++) {
//				sum += Mathf.Pow((1-alpha), i )*array[getIndex(jointIndex - i)];
//				
//			}
//			sum = alpha*sum;
			sum = alpha*array[getIndex(jointIndex)] + (1-alpha)*array[getIndex(jointIndex - 1)] + (1-alpha)*(1-alpha)*array[getIndex(jointIndex - 2)];
			break;
		case FILTER_NAME.DOUBLE_EXP_SMOOTHING:
			Vector3 trend = gamma*(arrayOutput[getIndex(jointOutputIndex-1)] -arrayOutput[getIndex(jointOutputIndex-2)]) + (1-gamma)*(gamma*(arrayOutput[getIndex(jointOutputIndex-3)] -arrayOutput[getIndex(jointOutputIndex-4)]));
			sum = alpha*array[getIndex(jointIndex)] + (1-alpha)*(arrayOutput[getIndex(jointIndex - 2)] + trend);
		
			break;
		case FILTER_NAME.MEDIAN:
			if (jointType == JOINT_TYPE.JOINT) 
			{
				jointsMedian.Sort ();
				sum = jointsMedian[jointsMedian.Count/2];
			}
			
			myvec.Sort ();
			myvec.RemoveAt (myvec.Count)			sum = alpha*array[getIndex(jointIndex)] + (1-alpha)*(arrayOutput[getIndex(jointIndex - 2)] + trend);
		
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
	
	// Controls circular array traversal
	private int getIndex(int index) 
	{
		if(index <0) {
			return index + numHistory;
		}
		else if(index >= numHistory) {
			return index - numHistory;
		}
		else
			return index;
	}
}
