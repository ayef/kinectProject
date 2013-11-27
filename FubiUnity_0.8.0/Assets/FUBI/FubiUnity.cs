using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using FubiNET;

public class FubiUnity : MonoBehaviour
{	
	
	//AA: Variables added for Filtering for Kinect Arm pointing Project:
	
	public bool m_bUseJointFiltering = false;
	public bool m_bUseVectorFiltering = true;
	public Filter filter = null;					// Our filter class
	public GameObject[] MyGameObjects;				// Gameobject array - currently empty
	Vector2 m_absPixelPosition = new Vector2(0, 0);	// GUI coordinate space coordinates for cursor
	Vector2 m_previousAbsPixelPosition = new Vector2(0, 0);	// GUI coordinate space coordinates for some previous value of the cursor poistion

	//AA: Variables for the filter menu checkboxes (Called 'toggles' in Unity)
	bool m_bUseSmoothingFilter = true;
	
	//AA: Variables for the Filter Output display
	private Texture2D m_filterOutputTexture;	// Texture used for drawing the filter outputs
	private int filterOutputLocX = Screen.width/3;	// Coordinates of top left corner of filter output texture
	private int filterOutputLocY = 10;					// 
	private int filterOutputWidth;	// Width and height of the kinect-writeable area
	private int filterOutputHeight;	// Initialized in Start()
	private int borderWidth = 10;	// Control witdh of border on all sides on right & bottom side of writeable area
	
	// To check if user has changed window size
	private int prevScreenWidth = Screen.width;
	private int prevScreenHeight = Screen.height;
	
	//AA: Colrs for colouring the different filter outputs
	Color [] bluePixels =  new Color [4];
	
	
	// Global properties
	public bool m_disableFubi = false;	
	public bool m_disableTrackingImage = false;
	public bool m_disableCursorWithGestures = true;	
    public bool m_disableTrackingImageWithSwipeMenu = true;

	// AA: fubi unused variables
	//	public bool m_disableSnapping = true;
    // The gesture symbols
    public MultiTexture2D[] m_gestureSymbols = new MultiTexture2D[0];
    Dictionary<string, MultiTexture2D> m_gestureSymbolDict = new Dictionary<string, MultiTexture2D>();

    // Cursor control properties
    public Texture2D m_defaultCursor;
    public float m_cursorScale = 0.15f;

	// AA: fubi unused variables
//	// Swipe gui elements
//    public AudioClip m_swipeSound;
//    public Texture2D m_swipeCenterNormal;
//    public Texture2D m_swipeCenterActive;
//    public Texture2D m_swipeButtonNormal;
//    public Texture2D m_swipeButtonActive;

  
    // The current valid FubiUnity instance
    public static FubiUnity instance = null;
    // The current user for all controls
    uint m_currentUser = 0;

	
	
	// The depth texture
    private Texture2D m_depthMapTexture;
    // And its pixels
    Color[] m_depthMapPixels;
    // The raw image from Fubi
    byte[] m_rawImage;
    // m_factor for clinching the image
    int m_factor = 3;
    // Depthmap resolution
    int m_xRes = 640;
    int m_yRes = 480;
    // The user image texture
    private Texture2D m_userImageTexture;

    // Vars for the cursor control stuff
    Vector2 m_relativeCursorPosition = new Vector2(0, 0);
    Rect m_mapping;
    float m_aspect = 1.33333f;
    float m_cursorAspect = 1.0f;
    double m_timeStamp = 0;
    double m_lastMouseClick = 0;
    Vector2 m_lastMousePos = new Vector2(0, 0);
    bool m_lastCursorChangeDoneByFubi = false;
    bool m_gotNewFubiCoordinates = false;
    bool m_lastCalibrationSucceded = false;
    Vector2 m_snappingCoords = new Vector2(-1, -1);
    bool m_buttonsDisplayed = false;
    // And the gestures
    bool m_gesturesDisplayed = false;
    bool m_gesturesDisplayedLastFrame = false;
    double m_lastGesture = 0;

//    // Swipe recognition vars
//    private bool m_swipeMenuActive = false;
    bool m_swipeMenuDisplayedLastFrame = false;
    bool m_swipeMenuDisplayed = false;
    // For the right hand
    private string[][] m_swipeRecognizers;
    private double m_lastSwipeRecognition = 0;
    private double[][] m_lastSwipeRecognitions;
    private uint m_handToFrontRecognizer;
	private double m_lastHandToFront = 0, m_handToFrontEnd = 0;
    // And for the left hand
    private string[][] m_leftSwipeRecognizers;
    private double m_lastLeftSwipeRecognition = 0;
    private double[][] m_lastLeftSwipeRecognitions;
    private uint m_leftHandToFrontRecognizer;
    private double m_lastLeftHandToFront = 0, m_leftHandToFrontEnd = 0;
    // Template for the swipe gesture recognizers
    string m_swipeCombinationXMLTemplate = @"<CombinationRecognizer name=""{0}"">
            <State minDuration=""0.2"" maxInterruptionTime=""0.1"" timeForTransition=""0.7"">
                <Recognizer name=""{1}""/>
                <Recognizer name=""{2}""/>
            </State>
            <State>
                <Recognizer name=""{3}""/>
            </State>
        </CombinationRecognizer>";	
	//AA: Resizes the writeable area when screen is resized
	void WriteableAreaResize() 
	{
		filterOutputWidth = Screen.width - filterOutputLocX - borderWidth ;
		filterOutputHeight = Screen.height - filterOutputLocY - borderWidth;
		
		if( m_filterOutputTexture == null)
			m_filterOutputTexture = new Texture2D(filterOutputWidth , filterOutputHeight);
		else
			m_filterOutputTexture.Resize (filterOutputWidth , filterOutputHeight);
	}	
    // Initialization
    void Start()
    {
        //AA: Filter visalization related initializations 
		filter = new Filter();
		WriteableAreaResize();
		bluePixels[0] = bluePixels[1] = bluePixels[2] = bluePixels[3] = Color.blue;
		FilterVisualization.DrawCircle(m_filterOutputTexture, m_filterOutputTexture.width/2, m_filterOutputTexture.height/2, 100, Color.yellow);
		
		// First set instance so Fubi.release will not be called while destroying old objects
        instance = this;
        // Remain this instance active until new one is created
        DontDestroyOnLoad(this);
		
		// Destroy old instance of Fubi
		object[] objects = GameObject.FindObjectsOfType(typeof(FubiUnity));
        if (objects.Length > 1)
        {          
            Destroy(((FubiUnity)objects[0]));
        }
// AA: delete, we don't need gestures
//        // Load gesture symbols (might be specific to that scene)
//		for (int i=0; i < m_gestureSymbols.Length; ++i)
//		{
//			if (m_gestureSymbols[i].Length == 1)
//				m_gestureSymbolDict.Add(m_gestureSymbols[i][0].name, m_gestureSymbols[i]);
//			else if (m_gestureSymbols[i].Length >= 1)
//				m_gestureSymbolDict.Add(m_gestureSymbols[i][0].name.Substring(0, m_gestureSymbols[i][0].name.Length-1), m_gestureSymbols[i]);
//		}

        m_lastMouseClick = 0;
        m_lastGesture = 0;
		
        // Init FUBI
		if (!m_disableFubi)
		{
            // Only init if not already done
            if (!Fubi.isInitialized())
            {
                Fubi.init(new FubiUtils.SensorOptions(new FubiUtils.StreamOptions(640, 480, 30), new FubiUtils.StreamOptions(640, 480, 30), 
					new FubiUtils.StreamOptions(-1, -1, -1), FubiUtils.SensorType.OPENNI2), new FubiUtils.FilterOptions());
                if (!Fubi.isInitialized())
                    Debug.Log("Fubi: FAILED to initialize Fubi!");
                else
                {
                    Debug.Log("Fubi: initialized!");
                }
            }
		}
		else
			m_disableTrackingImage = true;

		
        // Initialize debug image
        m_depthMapTexture = new Texture2D((int)(m_xRes / m_factor), (int)(m_yRes / m_factor), TextureFormat.RGBA32, false);
        m_depthMapPixels = new Color[(int)((m_xRes / m_factor) * (m_yRes / m_factor))];
        m_rawImage = new byte[(int)(m_xRes * m_yRes * 4)];

        m_userImageTexture = null;

		// Disable system cursor
        if (m_defaultCursor != null && m_disableFubi == false)
            Screen.showCursor = false;
        else
            Screen.showCursor = true;
		
        // Default mapping values
        m_mapping.x = -100.0f;
        m_mapping.y = 200.0f;
        m_mapping.height = 550.0f;



        // Get screen aspect
        m_aspect = (float)Screen.width / (float)Screen.height;

        // Calculated Map width with aspect
        m_mapping.width = m_mapping.height / m_aspect;

        if (Fubi.isInitialized())
        {
            // Clear old gesture recognizers
            Fubi.clearUserDefinedRecognizers();

            // And (re)load them
            if (Fubi.loadRecognizersFromXML("UnitySampleRecognizers.xml"))
                Debug.Log("Fubi: gesture recognizers 'BarRecognizers.xml' loaded!");
            else
                Debug.Log("Fubi: loading XML recognizers failed!");

            // load mouse control recognizers
            if (Fubi.loadRecognizersFromXML("MouseControlRecognizers.xml"))
                Debug.Log("Fubi: mouse control recognizers loaded!");
            else
                Debug.Log("Fubi: loading mouse control recognizers failed!");
        }

//AA: we don't need swipe code
//		// Recognizer for activating the swipe menu
//        m_handToFrontRecognizer = Fubi.addJointRelationRecognizer(FubiUtils.SkeletonJoint.RIGHT_HAND,
//                    FubiUtils.SkeletonJoint.RIGHT_SHOULDER, -200.0f, -200.0f, float.MinValue, 300.0f, 300.0f, -250.0f);
//        // Swipe recognizers for 1 to 12 options in a swipe menu
//        m_swipeRecognizers = new string[12][];
//        m_lastSwipeRecognitions = new double[12][];
//        for (uint i = 0; i < 12; ++i)
//        {
//            uint len = i + 1;
//            float rotAdd = 2.0f * Mathf.PI / len;
//            float maxAngleDiff = Mathf.Min(45.0f, 360.0f / len);
//            m_swipeRecognizers[i] = new string[len];
//            m_lastSwipeRecognitions[i] = new double[len];
//            for (uint j = 0; j < len; ++j)
//            {
//                float currRot = j * rotAdd;
//                if (i < 2)
//                    currRot += Mathf.PI / 2.0f;
//                float dirX = Mathf.Sin(currRot);
//                float dirY = Mathf.Cos(currRot);
//                m_lastSwipeRecognitions[i][j] = 0;
//                uint movRecIndex = Fubi.addLinearMovementRecognizer(
//                    FubiUtils.SkeletonJoint.RIGHT_HAND,
//                    FubiUtils.SkeletonJoint.RIGHT_SHOULDER,
//                    dirX, dirY, 0, 250.0f, float.MaxValue, false, -1, null, maxAngleDiff);
//                uint movPosRecIndex = Fubi.addJointRelationRecognizer(
//                    FubiUtils.SkeletonJoint.RIGHT_HAND,
//                    FubiUtils.SkeletonJoint.RIGHT_SHOULDER,
//                    (dirX < -0.05f) ? float.MinValue : -200.0f,
//                    (dirY < -0.05f) ? float.MinValue : -200.0f,
//                    float.MinValue,
//                    (dirX > 0.05f) ? float.MaxValue : 300.0f,
//                    (dirY > 0.05f) ? float.MaxValue : 300.0f,
//                    -125.0f);
//                uint stopRecIndex = Fubi.addJointRelationRecognizer(
//                    FubiUtils.SkeletonJoint.RIGHT_HAND,
//                    FubiUtils.SkeletonJoint.RIGHT_SHOULDER,
//                    (dirX > 0.05f) ? (150.0f * dirX) : float.MinValue,
//                    (dirY > 0.05f) ? (100.0f * dirY) : float.MinValue,
//                    float.MinValue,
//                    (dirX < -0.05f) ? (100.0f * dirX) : float.MaxValue,
//                    (dirY < -0.05f) ? (150.0f * dirY) : float.MaxValue,
//                    float.MaxValue);
//                string name = string.Format("swipeRec{0}", movRecIndex);
//                string combinationDef = string.Format(m_swipeCombinationXMLTemplate, name, movRecIndex, movPosRecIndex, stopRecIndex);
//                if (Fubi.addCombinationRecognizer(combinationDef))
//                    m_swipeRecognizers[i][j] = name;
//                else m_swipeRecognizers[i][j] = "";
//            }
//        }
//        // Now for the left hand
//        m_leftHandToFrontRecognizer = Fubi.addJointRelationRecognizer(FubiUtils.SkeletonJoint.LEFT_HAND,
//                    FubiUtils.SkeletonJoint.LEFT_SHOULDER, -300.0f, -200.0f, float.MinValue, 200.0f, 300.0f, -250.0f);
//        // Swipe recognizers for 1 to 12 options in a swipe menu
//        m_leftSwipeRecognizers = new string[12][];
//        m_lastLeftSwipeRecognitions = new double[12][];
//        for (uint i = 0; i < 12; ++i)
//        {
//            uint len = i + 1;
//            float rotAdd = 2.0f * Mathf.PI / len;
//            float maxAngleDiff = Mathf.Min(45.0f, 360.0f / len);
//            m_leftSwipeRecognizers[i] = new string[len];
//            m_lastLeftSwipeRecognitions[i] = new double[len];
//            for (uint j = 0; j < len; ++j)
//            {
//                float currRot = j * rotAdd;
//                if (i < 2)
//                    currRot += Mathf.PI / 2.0f;
//                float dirX = Mathf.Sin(currRot);
//                float dirY = Mathf.Cos(currRot);
//                m_lastLeftSwipeRecognitions[i][j] = 0;
//                uint movRecIndex = Fubi.addLinearMovementRecognizer(
//                    FubiUtils.SkeletonJoint.LEFT_HAND,
//                    FubiUtils.SkeletonJoint.LEFT_SHOULDER,
//                    dirX, dirY, 0, 250.0f, float.MaxValue, false, -1, null, maxAngleDiff);
//                uint movPosRecIndex = Fubi.addJointRelationRecognizer(
//                    FubiUtils.SkeletonJoint.LEFT_HAND,
//                    FubiUtils.SkeletonJoint.LEFT_SHOULDER,
//                    (dirX < -0.05f) ? float.MinValue : -300.0f,
//                    (dirY < -0.05f) ? float.MinValue : -200.0f,
//                    float.MinValue,
//                    (dirX > 0.05f) ? float.MaxValue : 200.0f,
//                    (dirY > 0.05f) ? float.MaxValue : 300.0f,
//                    -125.0f);
//                uint stopRecIndex = Fubi.addJointRelationRecognizer(
//                    FubiUtils.SkeletonJoint.LEFT_HAND,
//                    FubiUtils.SkeletonJoint.LEFT_SHOULDER,
//                    (dirX > 0.05f) ? (100.0f * dirX) : float.MinValue,
//                    (dirY > 0.05f) ? (100.0f * dirY) : float.MinValue,
//                    float.MinValue,
//                    (dirX < -0.05f) ? (150.0f * dirX) : float.MaxValue,
//                    (dirY < -0.05f) ? (150.0f * dirY) : float.MaxValue,
//                    float.MaxValue);
//                string name = string.Format("swipeLeftRec{0}", movRecIndex);
//                string combinationDef = string.Format(m_swipeCombinationXMLTemplate, name, movRecIndex, movPosRecIndex, stopRecIndex);
//                if (Fubi.addCombinationRecognizer(combinationDef))
//                    m_leftSwipeRecognizers[i][j] = name;
//                else m_leftSwipeRecognizers[i][j] = "";
//            }
//        }
    }

    // Update is called once per frame
    void Update()
    {
        // update FUBI
       	Fubi.updateSensor();

		// AA: Collision detection for our game object
		// Convert from GUI coordinates (Top-left: 0,0 to Bottom-right ) to Screen coordinates ((Bottom-left: 0,0 to Top-right )): 
		// Screen-coordinate.y = Screen.height - GUI-coordinate.y;
		Ray ray = Camera.mainCamera.ScreenPointToRay(new Vector2(m_absPixelPosition.x, Screen.height - m_absPixelPosition.y));
		
		// check for underlying objects
		RaycastHit hit;
		if(Physics.Raycast(ray, out hit))
		{
			foreach(GameObject obj in MyGameObjects)
			{
				if(hit.collider.gameObject == obj)
				{
					
					if (clickRecognized()) {
                		// an object was hit by the ray. Color it
						if(obj.renderer.material.color == Color.red) {
							obj.renderer.material.color = Color.blue;
							break;
						}
						else {
							obj.renderer.material.color = Color.red;	
							break;
						}
					}
				}
			}
		}

		
        // update flags if gestures or the swipe menu have been displayed
        if (m_gesturesDisplayed)
        {
            m_gesturesDisplayedLastFrame = true;
            m_gesturesDisplayed = false;
        }
        else
            m_gesturesDisplayedLastFrame = false;
        if (m_swipeMenuDisplayed)
        {
            m_swipeMenuDisplayedLastFrame = true;
            m_swipeMenuDisplayed = false;
        }
        else
            m_swipeMenuDisplayedLastFrame = false;

        // Render tracking image
		// AA: we don't need swipemenu related variables
        if (!m_disableTrackingImage && (!m_disableTrackingImageWithSwipeMenu || !m_swipeMenuDisplayedLastFrame))
		{
			uint renderOptions = (uint)(FubiUtils.RenderOptions.Default | FubiUtils.RenderOptions.DetailedFaceShapes | FubiUtils.RenderOptions.FingerShapes);
            Fubi.getImage(m_rawImage, FubiUtils.ImageType.Depth, FubiUtils.ImageNumChannels.C4, FubiUtils.ImageDepth.D8, renderOptions);
            Updatem_depthMapTexture();
		}
    }

    void MoveMouse(float mousePosX, float mousePosY, bool forceDisplay = false)
    {
        // TODO change texture for dwell
        Texture2D cursorImg = m_defaultCursor;
		Debug.Log ("In movemouse: mousePosX mousePosY " + mousePosX +" " + mousePosY );

        m_cursorAspect = (float)cursorImg.width / (float)cursorImg.height;
		float width = m_cursorScale * m_cursorAspect * (float)m_filterOutputTexture.height;
		float height = m_cursorScale * (float)m_filterOutputTexture.height;
		float x = mousePosX * (float)m_filterOutputTexture.width - 0.5f*width;
		float y = mousePosY * (float)m_filterOutputTexture.height - 0.5f*height;
		Debug.Log ("cursor x y " +  x+ " " + y);

		Rect pos = new Rect(filterOutputLocX + x, filterOutputLocY + y, width, height);
		GUI.depth = -3;
        GUI.Label(pos, cursorImg);

//AA: Old code for mapping active screen - can delete
//		float width = m_cursorScale * m_cursorAspect * (float)Screen.height;
//		float height = m_cursorScale * (float)Screen.height;
//		float x = mousePosX * (float)Screen.width - 0.5f*width;
//		float y = mousePosY * (float)Screen.height - 0.5f*height;
//		Rect pos = new Rect(x, y, width, height);
//		if ((m_buttonsDisplayed || forceDisplay) && m_disableFubi == false)
//		{
//			Debug.Log ("In movemouse: cursor width height " + width +" " + height + " x y " + x+ " " + y);
//			GUI.depth = -3;
//	        GUI.Label(pos, cursorImg);
//			m_buttonsDisplayed = false;
//		}
		m_absPixelPosition.x = x;
		m_absPixelPosition.y = y;

    }
	
	// Function added so that mouse input can be mapped to entire screen
	void MoveActualMouse(float mousePosX, float mousePosY, bool forceDisplay = false)
    {
        // TODO change texture for dwell
        Texture2D cursorImg = m_defaultCursor;
        m_cursorAspect = (float)cursorImg.width / (float)cursorImg.height;
		float width = m_cursorScale * m_cursorAspect * (float)m_filterOutputTexture.height;
		float height = m_cursorScale * (float)Screen.height;
		float x = mousePosX * (float)Screen.width - 0.5f*width;
		float y = mousePosY * (float)Screen.height - 0.5f*height;

		Rect pos = new Rect(x, y, width, height);
		GUI.depth = -3;
        GUI.Label(pos, cursorImg);

    }
	
	//AA: Draw the filter output
	void DrawFilterOutputs()
	{
		int factor =2;
		if(InBounds(m_filterOutputTexture , m_absPixelPosition ) )
		{
	       	m_filterOutputTexture.SetPixels((int)( m_absPixelPosition.x), (int)((filterOutputHeight - m_absPixelPosition.y) ), 2, 2, bluePixels);
		}
		
	}
	
	//AA: Checks whether position is inside the texture
	bool InBounds(Texture2D texture , Vector2 position ) 
	{
		if(position.x < texture.width - borderWidth && position.x >  borderWidth && position.y < texture.height - borderWidth && position.y >  borderWidth ) 
			return true;
		else
			return false;
	}

    // Upload the depthmap to the texture
    void Updatem_depthMapTexture()
    {
        int YScaled = m_yRes / m_factor;
        int XScaled = m_xRes / m_factor;
        int i = XScaled * YScaled - 1;
        int depthIndex = 0;
        for (int y = 0; y < YScaled; ++y)
        {
            depthIndex += (XScaled - 1) * m_factor * 4; // Skip lines
            for (int x = 0; x < XScaled; ++x, --i, depthIndex -= m_factor * 4)
            {
                m_depthMapPixels[i] = new Color(m_rawImage[depthIndex + 2] / 255.0f, m_rawImage[depthIndex + 1] / 255.0f, m_rawImage[depthIndex] / 255.0f, m_rawImage[depthIndex + 3] / 255.0f);
            }
            depthIndex += m_factor * (m_xRes + 1) * 4; // Skip lines
        }
        m_depthMapTexture.SetPixels(m_depthMapPixels);
        m_depthMapTexture.Apply();
    }

    // Called for rendering the gui
    void OnGUI()
    {
		// AA: Position the depth image so the user can see the kinect output
        if (!m_disableTrackingImage && (!m_disableTrackingImageWithSwipeMenu || !m_swipeMenuDisplayedLastFrame))
		{
	        // Debug image
			GUI.depth = -4;
	        GUI.DrawTexture(new Rect(25, Screen.height-m_yRes/m_factor - 25, m_xRes / m_factor, m_yRes / m_factor), m_depthMapTexture);
	        //GUI.DrawTexture(new Rect(Screen.width-m_xRes/m_factor, Screen.height-m_yRes/m_factor, m_xRes / m_factor, m_yRes / m_factor), m_depthMapTexture);
		}
		
		//AA: add the GUI elements
		
		GUI.Box (new Rect(25, 25, 200, 200) , "Filter Menu");
		
		m_bUseSmoothingFilter = GUI.Toggle(new Rect(35,50,150,30), m_bUseSmoothingFilter, "Use Smoothing Filter");
		if( GUI.Button(new Rect(35, 100,150,30), "Clear"))
		{
			WriteableAreaResize();
			FilterVisualization.DrawCircle(m_filterOutputTexture, m_filterOutputTexture.width/2, m_filterOutputTexture.height/2, 100, Color.yellow);
			
		}
		if( prevScreenWidth != Screen.width || prevScreenHeight != Screen.height) 
		{
			// Resize writeable area, redraw the circle
			WriteableAreaResize();
			prevScreenWidth = Screen.width;
			prevScreenHeight = Screen.height;
			FilterVisualization.DrawCircle(m_filterOutputTexture, m_filterOutputTexture.width/2, m_filterOutputTexture.height/2, 100, Color.yellow);
		}

		//AA: Draw the writeable area
		m_filterOutputTexture.Apply();
		GUI.DrawTexture(new Rect(filterOutputLocX , filterOutputLocY, m_filterOutputTexture.width, m_filterOutputTexture.height), m_filterOutputTexture);
		
		
		// Cursor
		m_gotNewFubiCoordinates = false;
        if (Fubi.isInitialized())
        {
			// Take closest user
            uint userID = Fubi.getClosestUserID();
			if (userID != m_currentUser)
			{
				m_currentUser = userID;
				m_lastCalibrationSucceded = false;
			}
            if (userID > 0)
            {
				if (!m_lastCalibrationSucceded)
					m_lastCalibrationSucceded = calibrateCursorMapping(m_currentUser);
                FubiUtils.SkeletonJoint joint = FubiUtils.SkeletonJoint.RIGHT_HAND;
                FubiUtils.SkeletonJoint relJoint = FubiUtils.SkeletonJoint.RIGHT_SHOULDER;
                //if (leftHand)
                //{
                //    joint = FubiUtils.SkeletonJoint.LEFT_HAND;
                //    relJoint = FubiUtils.SkeletonJoint.LEFT_SHOULDER;
                //}
			
				// Get hand and shoulder position and check their confidence
                double timeStamp;
				float handX, handY, handZ, confidence;
                Fubi.getCurrentSkeletonJointPosition(userID, joint, out handX, out handY, out handZ, out confidence, out timeStamp);
                if (confidence > 0.5f)
                {
                    float relX, relY, relZ;
                    Fubi.getCurrentSkeletonJointPosition(userID, relJoint, out relX, out relY, out relZ, out confidence, out timeStamp);
					if (confidence > 0.5f)
                    {
						
						// AA: Filtering should happen here for the hand and relative joints separately
						// If true, use the smoothed joints for calculating screen coordinates

						if(m_bUseJointFiltering) {
							Vector3 handPos = filter.Update(new Vector3(handX, handY, handZ), Filter.JOINT_TYPE.JOINT);
							Vector3 relJointPos = filter.Update(new Vector3(relX, relY, relZ), Filter.JOINT_TYPE.RELATIVEJOINT);
//							Debug.Log ("hand x y z " + handX + " " + handY + " " + handZ);
//							Debug.Log ("handpos x y z " + handPos.x + " " + handPos.y + " " + handPos.z);
							handZ = handPos.z;
							handY = handPos.y;
							handX = handPos.x;
							
							relZ = relJointPos.z;
							relY = relJointPos.y;
							relX = relJointPos.x;
							
						}
						// AA: End  
						
						// Take relative coordinates
						float zDiff = handZ - relZ;
						float yDiff = handY - relY;
						float xDiff = handX - relX;
						// Check if hand is enough in front of shoulder
						if ((yDiff >0 && zDiff < -150.0f) || (Mathf.Abs(xDiff) > 150.0f && zDiff < -175.0f) || zDiff < -225.0f)
						{
							// Now get the possible cursor position                       
	
	                        // Convert to screen coordinates
	                        float newX, newY;
	                        float mapX = m_mapping.x;
	                        //if (leftHand)
	                        //    // Mirror x  area for left hand
	                        //    mapX = -m_mapping.x - m_mapping.width;
	                        newX = (xDiff - mapX) / m_mapping.width;
	                        newY = (m_mapping.y - yDiff) / m_mapping.height; // Flip y for the screen coordinates

	                        // Filtering
	                        // New coordinate is weighted more if it represents a longer distance change
	                        // This should reduce the lagging of the cursor on higher distances, but still filter out small jittering
	                        float changeX = newX - m_relativeCursorPosition.x;
	                        float changeY = newY - m_relativeCursorPosition.y;
	
	                        if (changeX != 0 || changeY != 0 && timeStamp != m_timeStamp)
	                        {
	                            float changeLength = Mathf.Sqrt(changeX * changeX + changeY * changeY);
	                            float filterFactor = changeLength; //Mathf.Sqrt(changeLength);
	                            if (filterFactor > 1.0f) {
									 filterFactor = 1.0f;
//									Debug.Log ("filterfactor is 1");
								}
								else{
									//Debug.Log ("filterfactor is " + filterFactor);
									
								}
								
	                            
	
	                            // Apply the tracking to the current position with the given filter factor
								// AA: Filtering should happen here for joint-to-relativejoint (VECTOR) filtering
								// AA: filtering code
								
								Vector2 tempNew = new Vector2(newX,newY);
								
								// If true, use the calculated factor for smoothing, else just use the new
								if(m_bUseVectorFiltering) {
									//filterFactor
									m_relativeCursorPosition = filter.Update(m_relativeCursorPosition, tempNew, filterFactor);
								}
								else {	// Just give equal weight to both
									m_relativeCursorPosition = filter.Update(m_relativeCursorPosition, tempNew, 0.5f);
									
								}
								
	                            
								
	                            m_timeStamp = timeStamp;
	
	                            // Send it, but only if it is more or less within the screen
								if (m_relativeCursorPosition.x > -0.1f && m_relativeCursorPosition.x < 1.1f
									&& m_relativeCursorPosition.y > -0.1f && m_relativeCursorPosition.y < 1.1f)
								{
									// AA: Disable snapping
//									if (!m_disableSnapping && m_snappingCoords.x >=0 && m_snappingCoords.y >= 0)
//									{
//										MoveMouse(m_snappingCoords.x, m_snappingCoords.y);
//										m_snappingCoords.x = -1;
//										m_snappingCoords.y = -1;
//									}
//									else
									
									MoveMouse(m_relativeCursorPosition.x, m_relativeCursorPosition.y);
									DrawFilterOutputs();
									m_gotNewFubiCoordinates = true;
									m_lastCursorChangeDoneByFubi = true;
								}
	                        }
						}
                    }
                }
            }
        }
        // AA: FUBI does not move mouse if the confidence value is too low 
		
		if (!m_gotNewFubiCoordinates)	// AA: this only executes when input is coming from mouse
        {
			// Got no mouse coordinates from fubi this frame
            Vector2 mousePos = Input.mousePosition;
			// Only move mouse if it wasn't changed by fubi the last time or or it really has changed
			if (!m_lastCursorChangeDoneByFubi || mousePos != m_lastMousePos)
			{
				//AA: Old code for cursor placement
            	m_relativeCursorPosition.x = mousePos.x / (float)Screen.width;
				m_relativeCursorPosition.y = 1.0f - (mousePos.y / (float)Screen.height);
            	// Get mouse X and Y position as a percentage of screen width and height
            	MoveActualMouse(m_relativeCursorPosition.x, m_relativeCursorPosition.y, true);
				m_lastMousePos = mousePos;
				m_lastCursorChangeDoneByFubi = false;
			}
        }
    }

    // Called on deactivation
    void OnDestroy()
    {
		if (this == instance)
		{
			Fubi.release();
        	Debug.Log("Fubi released!");
		}
    }
	
	static bool rectContainsCursor(Rect r)
	{
		// convert to relative screen coordinates
		r.x /= (float)Screen.width;
		r.y /= (float)Screen.height;
		r.width /= (float)Screen.width;
		r.height /= (float)Screen.height;
		
		// get cursor metrics
		float cursorWHalf = instance.m_cursorScale * instance.m_cursorAspect / 2.0f;
		float cursorHHalf = instance.m_cursorScale / 2.0f;
		Vector2 cursorCenter = instance.m_relativeCursorPosition;
		
		// check whether it is inside
		return (instance.m_gotNewFubiCoordinates &&
				(r.Contains(cursorCenter)
				 || r.Contains( cursorCenter + new Vector2(-cursorWHalf, -cursorHHalf) )
				 || r.Contains( cursorCenter + new Vector2(cursorWHalf, cursorHHalf) )
				 || r.Contains( cursorCenter + new Vector2(cursorWHalf, -cursorHHalf) )
				 || r.Contains( cursorCenter + new Vector2(-cursorWHalf, cursorHHalf) ) ));
	}

    static private bool clickRecognized()
    {
        bool click = false;
        if (Fubi.getCurrentTime() - instance.m_lastMouseClick > 0.5f)
        {
            uint userID = instance.m_currentUser;
            if (userID > 0)
            {
                // Check for mouse click as defined in xml
                FubiTrackingData[] userStates;
                if (Fubi.getCombinationRecognitionProgressOn("mouseClick", userID, out userStates, false) == FubiUtils.RecognitionResult.RECOGNIZED)
                {
                    if (userStates != null && userStates.Length > 0)
                    {
                        double clickTime = userStates[userStates.Length - 1].timeStamp;
                        // Check that click occured no longer ago than 1 second
                        if (Fubi.getCurrentTime() - clickTime < 1.0f)
                        {
                            click = true;
                            instance.m_lastMouseClick = clickTime;
                            // Reset all recognizers
                            Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, userID, false);
                        }
                    }
                }
                
                if (!click)
                    Fubi.enableCombinationRecognition("mouseClick", userID, true);

                if (Fubi.recognizeGestureOn("mouseClick", userID) == FubiUtils.RecognitionResult.RECOGNIZED)
                {
                    Debug.Log("Mouse click recognized.");
                    click = true;
                    instance.m_lastMouseClick = Fubi.getCurrentTime();
                    // Reset all recognizers
                    Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, userID, false);
                }
            }
        }
        return click;
    }
// AA: FUBI button not required in this demo
//	static public bool FubiButton(Rect r, string text, GUIStyle style)
//	{
//		bool cursorDisabled = instance.m_disableCursorWithGestures && instance.m_gesturesDisplayedLastFrame;
//		
//		instance.m_buttonsDisplayed = !cursorDisabled;
//		GUI.depth = -2;
//		bool click = false;
//		Rect checkRect = new Rect();
//		checkRect.x = r.x - r.height;
//		checkRect.y = r.y - r.height;
//		checkRect.width = r.width + 2*r.height;
//		checkRect.height = r.height + 2*r.height;
//		if (!cursorDisabled && rectContainsCursor(checkRect))
//		{
//			instance.m_snappingCoords = new Vector2(r.center.x / Screen.width, r.center.y / Screen.height);
//			GUI.Button(r, text, style.name+"-hover");
//            if (clickRecognized())
//                click = true;            
//		}
//		else
//		{
//			click = GUI.Button(r, text, style);
//		}
//		return click;
//	}

// AA: Removing this function disables click functionality so have not commented it out
	static public bool FubiGesture(Rect r, string name, GUIStyle style)
	{
		GUI.depth = -2;
		instance.m_gesturesDisplayed = true;
		
		if (instance.m_gestureSymbolDict.ContainsKey(name))
		{
            MultiTexture2D animation = instance.m_gestureSymbolDict[name];
            int index = (int)(Time.realtimeSinceStartup * animation.animationFps) % animation.Length;
            GUI.DrawTexture(r, animation[index], ScaleMode.ScaleToFit);
            if (GUI.Button(r, "", style))
                return true;
            if (instance.m_disableCursorWithGestures && instance.m_gesturesDisplayedLastFrame
                && rectContainsCursor(r) && clickRecognized())
                return true;
		}

        if (Fubi.getCurrentTime() - instance.m_lastGesture > 0.8f)
        {
            uint userID = instance.m_currentUser;
            if (userID > 0)
            {
                FubiTrackingData[] userStates;
                if (Fubi.getCombinationRecognitionProgressOn(name, userID, out userStates, false) == FubiUtils.RecognitionResult.RECOGNIZED && userStates.Length > 0)
                {
                    double time = userStates[userStates.Length - 1].timeStamp;
                    // Check if gesture did not happen longer ago then 1 second
                    if (Fubi.getCurrentTime() - time < 1.0f)
                    {
                        instance.m_lastGesture = time;
                        // Reset all recognizers
                        Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, userID, false);
                        return true;
                    }
                }
                
                // Unsuccesfull recognition so start the recognizer for the next recognition
                Fubi.enableCombinationRecognition(name, userID, true);

                if (Fubi.recognizeGestureOn(name, userID) == FubiUtils.RecognitionResult.RECOGNIZED)
                {
                    instance.m_lastGesture = Fubi.getCurrentTime();
                    // Reset all recognizers
                    Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, userID, false);
                    return true;
                }
            }
        }
		
		return false;
	}

//AA: We don't need the FubiSwipeMenu. Removed 'FubiSwipeMenu(Vector2 center, float radius, string[] options, GUIStyle optionStyle, GUIStyle centerStyle)'
//    static public string FubiSwipeMenu(Vector2 center, float radius, string[] options, GUIStyle optionStyle, GUIStyle centerStyle)
//    {
//        if (instance)
//            return instance.DisplayFubiSwipeMenu(center, radius, options, optionStyle, centerStyle);
//        return "";
//    }

//    static public bool FubiCroppedUserImage(int x, int y, bool forceReload = false)
//    {
//        if (instance)
//            return instance.DisplayFubiCroppedUserImage(x, y, forceReload);
//        return false;
//    }
	
	// AA: The 'DisplayFubiCroppedUserImage' function could be used to explain the problems with filtering
    private bool DisplayFubiCroppedUserImage(int x, int y, bool forceReload)
    {
        if (m_userImageTexture == null || forceReload == true)
        {
            // First get user image
            Fubi.getImage(m_rawImage, FubiUtils.ImageType.Color, FubiUtils.ImageNumChannels.C4, FubiUtils.ImageDepth.D8, (uint)FubiUtils.RenderOptions.None, (uint)FubiUtils.JointsToRender.ALL_JOINTS, FubiUtils.DepthImageModification.Raw, m_currentUser, FubiUtils.SkeletonJoint.HEAD, true);

            // Now look for the image borders
            int xMax = m_xRes; int yMax = m_yRes;
            int index = 0;
            for (int x1 = 0; x1 < m_xRes; ++x1, index += 4)
            {
                if (m_rawImage[index + 3] == 0)
                {
                    xMax = x1;
                    break;
                }
            }
            index = 0;
            for (int y1 = 0; y1 < m_yRes; ++y1, index += (m_xRes + 1) * 4)
            {
                if (m_rawImage[index + 3] == 0)
                {
                    yMax = y1;
                    break;
                }
            }

            // Create the texture
            m_userImageTexture = new Texture2D(xMax, yMax, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[xMax*yMax];

            // And copy the pixels
            int i = xMax * yMax - 1;
            index = 0;
            for (int yy = 0; yy < yMax; ++yy)
            {
                index += (xMax - 1) * 4; // Move to line end
                for (int xx = 0; xx < xMax; ++xx, --i, index -= 4)
                {
                    pixels[i] = new Color(m_rawImage[index] / 255.0f, m_rawImage[index + 1] / 255.0f, m_rawImage[index + 2] / 255.0f, m_rawImage[index + 3] / 255.0f);
                }
                index += (m_xRes + 1) * 4; // Move to next line
            }

            m_userImageTexture.SetPixels(pixels);
            m_userImageTexture.Apply();
        }

        GUI.depth = -4;
        GUI.DrawTexture(new Rect(x, y, m_userImageTexture.width, m_userImageTexture.height), m_userImageTexture);

        return false;
    }

//    private string DisplayFubiSwipeMenu(Vector2 center, float radius, string[] options, GUIStyle optionStyle, GUIStyle centerStyle)
//    {
//        if (!(radius > 0 && options.Length > 0 && options.Length <= m_swipeRecognizers.Length))
//        {
//            Debug.LogWarning("FubiSwipeMenu called with incorrect parameters!");
//            Debug.DebugBreak();
//            return "";
//        }
//
//        GUI.depth = -2;
//        string selection = "";
//        m_swipeMenuDisplayed = true;
//		
//		ScaleMode smode = /*(options.Length >= 2) ? ScaleMode.StretchToFill :*/ ScaleMode.ScaleToFit;
//
//        // Display center and explanation text
//        float centerRad = radius * 0.25f;
//        Rect centerRect = new Rect(center.x - centerRad, center.y - centerRad, 2.0f * centerRad, 2.0f * centerRad);
//
//        // Check for right hand in front for general activation
//        if (m_currentUser > 0 && Fubi.recognizeGestureOn(m_handToFrontRecognizer, m_currentUser) == FubiUtils.RecognitionResult.RECOGNIZED)
//			m_lastHandToFront = Fubi.getCurrentTime();
//        else if (Fubi.getCurrentTime() - m_lastHandToFront > 1.2f)
// 			m_handToFrontEnd = Fubi.getCurrentTime();
//        // Check for left hand in front for general activation
//        if (m_currentUser > 0 && Fubi.recognizeGestureOn(m_leftHandToFrontRecognizer, m_currentUser) == FubiUtils.RecognitionResult.RECOGNIZED)
//            m_lastLeftHandToFront = Fubi.getCurrentTime();
//        else if (Fubi.getCurrentTime() - m_lastLeftHandToFront > 1.2f)
//            m_leftHandToFrontEnd = Fubi.getCurrentTime();
//
//        bool withinInteractionFrame = m_currentUser > 0 && (m_lastHandToFront - m_handToFrontEnd > 0.5 || m_lastLeftHandToFront - m_leftHandToFrontEnd > 0.5);
//		if (withinInteractionFrame)
//		{
//			if (!m_swipeMenuActive)
//			{
//				m_swipeMenuActive = true;
//				Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, m_currentUser, false);
//			}				
//            GUI.DrawTexture(centerRect, m_swipeCenterActive, smode);
//            GUI.Label(centerRect, "Swipe for \nselection", centerStyle.name+"-hover");
//		}
//		else
//		{
//			m_swipeMenuActive = false;
//			GUI.DrawTexture(centerRect, m_swipeCenterNormal, smode);
//            if (m_lastHandToFront > m_handToFrontEnd || m_lastLeftHandToFront > m_leftHandToFrontEnd)
//			{
//				GUI.Label(centerRect, "Hold arm", centerStyle);
//			}
//			else
//	        	GUI.Label(centerRect, "Stretch arm \nto front", centerStyle);
//		}
//
//        // Display the options and check their recognizers 
//        float rotAdd = 360.0f / options.Length;
//        float currRot = (options.Length > 2) ? 0 : 90.0f;
//        Rect buttonRect = new Rect();
//        buttonRect.height = radius * 0.35f;
//        buttonRect.width = (options.Length > 2) ? (Mathf.PI * (radius / 2.0f) / options.Length) : buttonRect.height;
//        buttonRect.y = center.y - (0.65f* radius);
//        buttonRect.x = center.x - (0.5f * buttonRect.width);
//        for (uint i = 0; i < options.Length; ++i)
//        {
//			Rect textRect = new Rect(buttonRect);
//			textRect.y = center.y - radius;
//			textRect.height *= 0.5f;
//            string text = options[i];
//            GUI.matrix = Matrix4x4.identity;
//            bool selected = false;
//            int recognizerGroup = options.Length - 1;
//            uint recognizerIndex = i;
//
//
//            if (Fubi.getCurrentTime() - m_lastSwipeRecognitions[recognizerGroup][recognizerIndex] < 0.5
//                || Fubi.getCurrentTime() - m_lastLeftSwipeRecognitions[recognizerGroup][recognizerIndex] < 0.5) // last recognition not longer than 0.5 seconds ago
//            {
//                GUIUtility.RotateAroundPivot(currRot, center);
//                GUI.DrawTexture(buttonRect, m_swipeButtonActive, smode);
//                Vector3 newPos = GUI.matrix.MultiplyPoint(new Vector3(textRect.center.x, textRect.center.y));
//                textRect.x = newPos.x - textRect.width / 2.0f;
//                textRect.y = newPos.y - textRect.height / 2.0f;
//                GUI.matrix = Matrix4x4.identity;
//                selected = GUI.Button(textRect, text, optionStyle.name+"-hover");
//            }
//            else // Display button also usable for mouse interaction
//            {
//                GUIUtility.RotateAroundPivot(currRot, center);
//                GUI.DrawTexture(buttonRect, m_swipeButtonNormal, smode);
//                Vector3 newPos = GUI.matrix.MultiplyPoint(new Vector3(textRect.center.x, textRect.center.y));
//                textRect.x = newPos.x - textRect.width / 2.0f;
//                textRect.y = newPos.y - textRect.height / 2.0f;
//                GUI.matrix = Matrix4x4.identity;
//                selected = GUI.Button(textRect, text, optionStyle);
//            }
//
//
//			// Check for full swipe
//            // Of right hand
//			if (!selected && m_currentUser > 0
//                && withinInteractionFrame
//                && Fubi.getCurrentTime() - m_lastSwipeRecognition > 1.0f) // at least one second between to swipes
//            {
//                selected = Fubi.getCombinationRecognitionProgressOn(m_swipeRecognizers[recognizerGroup][recognizerIndex], m_currentUser, false) == FubiUtils.RecognitionResult.RECOGNIZED;
//                if (selected)
//				{
//					m_swipeMenuActive = false;
//                    Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, m_currentUser, false);
//                    m_lastSwipeRecognitions[recognizerGroup][recognizerIndex] = m_lastSwipeRecognition = Fubi.getCurrentTime();
//				}
//                else
//                    Fubi.enableCombinationRecognition(m_swipeRecognizers[options.Length - 1][i], m_currentUser, true);
//            }
//
//            // Or left hand
//            if (!selected && m_currentUser > 0
//                && withinInteractionFrame
//                && Fubi.getCurrentTime() - m_lastLeftSwipeRecognition > 1.0f) // at least one second between to swipes
//            {
//                selected = Fubi.getCombinationRecognitionProgressOn(m_leftSwipeRecognizers[recognizerGroup][recognizerIndex], m_currentUser, false) == FubiUtils.RecognitionResult.RECOGNIZED;
//                if (selected)
//                {
//                    m_swipeMenuActive = false;
//                    Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, m_currentUser, false);
//                    m_lastLeftSwipeRecognitions[recognizerGroup][recognizerIndex] = m_lastLeftSwipeRecognition = Fubi.getCurrentTime();
//                }
//                else
//                    Fubi.enableCombinationRecognition(m_leftSwipeRecognizers[options.Length - 1][i], m_currentUser, true);
//            }
//
//
//            if (selected)
//            {
//                selection = text;
//                if (m_swipeSound && m_swipeSound.isReadyToPlay)
//                    AudioSource.PlayClipAtPoint(m_swipeSound, new Vector3(0,0,0), 1.0f);
//                break;
//            }
//            currRot += rotAdd;
//        }
//        return selection;
//    }
//

	bool calibrateCursorMapping(uint id)
    {
		m_aspect = (float)Screen.width / (float)Screen.height;
        if (id > 0)
        {
            FubiUtils.SkeletonJoint elbow = FubiUtils.SkeletonJoint.RIGHT_ELBOW;
            FubiUtils.SkeletonJoint shoulder = FubiUtils.SkeletonJoint.RIGHT_SHOULDER;
            FubiUtils.SkeletonJoint hand = FubiUtils.SkeletonJoint.RIGHT_HAND;

            float confidence;
            double timeStamp;
            float elbowX, elbowY, elbowZ;
            Fubi.getCurrentSkeletonJointPosition(id, elbow, out elbowX, out elbowY, out elbowZ, out confidence, out timeStamp);
            if (confidence > 0.5f)
            {
                float shoulderX, shoulderY, shoulderZ;
                Fubi.getCurrentSkeletonJointPosition(id, shoulder, out shoulderX, out shoulderY, out shoulderZ, out confidence, out timeStamp);
                if (confidence > 0.5f)
                {
                    double dist1 = Mathf.Sqrt(Mathf.Pow(elbowX - shoulderX, 2) + Mathf.Pow(elbowY - shoulderY, 2) + Mathf.Pow(elbowZ - shoulderZ, 2));
                    float handX, handY, handZ;
                    Fubi.getCurrentSkeletonJointPosition(id, hand, out handX, out handY, out handZ, out confidence, out timeStamp);
                    if (confidence > 0.5f)
                    {
                        double dist2 = Mathf.Sqrt(Mathf.Pow(elbowX - handX, 2) + Mathf.Pow(elbowY - handY, 2) + Mathf.Pow(elbowZ - handZ, 2));
                        m_mapping.height = (float)(dist1 + dist2);
                        // Calculate all others in depence of maph
                        m_mapping.y = 200.0f / 550.0f * m_mapping.height;
                        m_mapping.width = m_mapping.height / m_aspect;
                        m_mapping.x = -100.0f / (550.0f / m_aspect) * m_mapping.width;
						Debug.Log ("m_mapping.x=" + m_mapping.x + "m_mapping.y=" + m_mapping.y + "m_mapping.height=" + m_mapping.height + "m_mapping.width=" + m_mapping.width);
						return true;
                    }
                }
            }
        }
		return false;
    }
	
	
	// AA: Code for drawing lines from: http://wiki.unity3d.com/index.php?title=DrawLine
	// http://wiki.unity3d.com/index.php?title=TextureDrawLine
	void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
	{
	 	int dy = (int)(y1-y0);
		int dx = (int)(x1-x0);
	 	int stepx, stepy;
	 
		if (dy < 0) {dy = -dy; stepy = -1;}
		else {stepy = 1;}
		if (dx < 0) {dx = -dx; stepx = -1;}
		else {stepx = 1;}
		dy <<= 1;
		dx <<= 1;
	 
		float fraction = 0;
	 
		tex.SetPixel(x0, y0, col);
		if (dx > dy) {
			fraction = dy - (dx >> 1);
			while (Mathf.Abs(x0 - x1) > 1) {
				if (fraction >= 0) {
					y0 += stepy;
					fraction -= dx;
				}
				x0 += stepx;
				fraction += dy;
				tex.SetPixel(x0, y0, col);
			}
		}
		else {
			fraction = dx - (dy >> 1);
			while (Mathf.Abs(y0 - y1) > 1) {
				if (fraction >= 0) {
					x0 += stepx;
					fraction -= dy;
				}
				y0 += stepy;
				fraction += dx;
				tex.SetPixel(x0, y0, col);
			}
		}
	}
}