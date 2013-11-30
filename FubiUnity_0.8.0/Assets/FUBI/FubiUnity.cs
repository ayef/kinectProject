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
	bool m_bUseSimpleAverage = true;
	bool m_bUseMovingAverage = false;
	bool m_bUseSimpleAverage5 = false;
	bool m_bDblMovingAverage = false;
	bool m_bUseExpSmoothing = false;
	bool m_bUseDblExpSmoothing = true;
	bool m_bUseMedian = false;
	bool m_bUseNone = true;
	bool m_bUseCombination1 = true;		// Hand joint filtered by simple avg, shoulder joint by median
	bool m_bUseCombination2 = true;		// Hand joint filtered by double moving average, shoulder joint by median
	bool m_bUseAdaptive = true;			// Adaptive double exponential
	
	
	int m_principalCursor = 0;	// Which filter to associatethe cursor with
	bool m_firstExecution = true; // Controls adding of textures for each filter
	
	//AA: Variables for the Filter Visualization display
	FilterVisualization fv = null;
	FilterManager fm = null;
	
	// To check if user has changed window size
	private int prevScreenWidth = Screen.width;
	private int prevScreenHeight = Screen.height;

	// To display colours against the filters
	public Texture2D[] m_colorTextures;
	private Dictionary<string, Texture2D > m_colorTextureDictionary;
	// Global properties
	public bool m_disableFubi = false;	
	public bool m_disableTrackingImage = false;
	public bool m_disableCursorWithGestures = true;	
    public bool m_disableTrackingImageWithSwipeMenu = true;

	// AA: Note - fubi unused variables
	//	public bool m_disableSnapping = true;
    // The gesture symbols
    public MultiTexture2D[] m_gestureSymbols = new MultiTexture2D[0];
    Dictionary<string, MultiTexture2D> m_gestureSymbolDict = new Dictionary<string, MultiTexture2D>();

    // Cursor control properties
    public Texture2D m_defaultCursor;
    public Texture2D m_defaultMouseCursor;
    public float m_cursorScale = 0.1f;

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
		fv.Initialise();
	}	
    
	// Initialization
    void Start()
    {
        //AA: Filter visalization related initializations 
		filter = new Filter();
		fv = new FilterVisualization();
		fm = new FilterManager();
		fv.Initialise();
		fv.DrawCircle();

		m_colorTextureDictionary = new Dictionary<string, Texture2D>();
		foreach(Texture2D tex in m_colorTextures) 
		{
			Debug.Log(" tex name: " + tex.name);
			m_colorTextureDictionary.Add (tex.name, tex);
		}
		
		LoadFilters();
		

		
		
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
		// AA: This 
        if (!m_disableTrackingImage && (!m_disableTrackingImageWithSwipeMenu || !m_swipeMenuDisplayedLastFrame))
		{
			uint renderOptions = (uint)(FubiUtils.RenderOptions.Default | FubiUtils.RenderOptions.DetailedFaceShapes | FubiUtils.RenderOptions.FingerShapes);
            Fubi.getImage(m_rawImage, FubiUtils.ImageType.Depth, FubiUtils.ImageNumChannels.C4, FubiUtils.ImageDepth.D8, renderOptions);
            Updatem_depthMapTexture();
		}
    }

    void MoveMouse(float mousePosX, float mousePosY, int i = 0)
    {
        // TODO change texture for dwell
        Texture2D cursorImg = m_defaultCursor;
		//Debug.Log ("In movemouse: mousePosX mousePosY " + mousePosX +" " + mousePosY );

        m_cursorAspect = (float)cursorImg.width / (float)cursorImg.height;
		float width = m_cursorScale * m_cursorAspect * (float)fv.filterOutputHeight;
		float height = m_cursorScale * (float)fv.filterOutputHeight;
		float x = mousePosX * (float)fv.filterOutputWidth - 0.5f*width;
		float y = mousePosY * (float)fv.filterOutputHeight - 0.5f*height;
		//Debug.Log ("cursor x y " +  x+ " " + y);
		if( i == m_principalCursor) 	// Display cursor for one of the filters only
		{
			Rect pos = new Rect(fv.filterOutputLocX + x, fv.filterOutputLocY + y, width, height);
			//GUI.depth = -3;
	        GUI.Label(pos, cursorImg);
			
		}
		m_previousAbsPixelPosition = fm.absPixelPosition[i];
		m_absPixelPosition.x = x;
		m_absPixelPosition.y = y;

    }
	
	// Function added so that mouse input can be mapped to entire screen
	void MoveActualMouse(float mousePosX, float mousePosY, bool forceDisplay = false)
    {
        // TODO change texture for dwell
        Texture2D cursorImg = m_defaultMouseCursor;
        m_cursorAspect = (float)cursorImg.width / (float)cursorImg.height;
		float width = m_cursorScale * m_cursorAspect * (float)fv.filterOutputHeight;
		float height = m_cursorScale * (float)Screen.height;
		float x = mousePosX * (float)Screen.width - 0.5f*width;
		float y = mousePosY * (float)Screen.height - 0.5f*height;

		Rect pos = new Rect(x, y, width, height);
		GUI.depth = -3;
        GUI.Label(pos, cursorImg);

    }
	
	//AA: Draw the filter output
	void DrawFilterOutputs(int i)
	{
		int factor = 2;
		if(InBounds(fv , m_absPixelPosition ) )
		{
	       	fv.SetPixels(m_absPixelPosition, fm.colors[i]);
			fv.DrawLine(0, (int) fm.prevAbsPixelPosition[i].x, (int) fm.prevAbsPixelPosition[i].y, (int)m_absPixelPosition.x, (int)m_absPixelPosition.y, fm.colors[i]);
		}
		
	}
	
	//AA: Checks whether position is inside the texture
	bool InBounds(FilterVisualization fVis , Vector2 position ) 
	{
		if(position.x < fVis.filterOutputWidth - fVis.borderWidth && position.x >  fVis.borderWidth && position.y < fVis.filterOutputHeight - fVis.borderWidth && position.y >  fVis.borderWidth ) 
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
	
	//AA: Load the filters according to GUI selections
	void LoadFilters() 
	{
		fm.Clear();

		m_previousAbsPixelPosition = m_absPixelPosition = Vector2.zero;

		if(m_bUseSimpleAverage == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.SIMPLE_AVG;
			f.numHistory = 10;		// Number of values to consider in mean
			fm.AddFilter (f);
		}

		if(m_bUseMovingAverage == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.MOVING_AVG;
			f.numHistory = 10;		// Number of values to consider in moving average
			fm.AddFilter (f);
		}
	
		if(m_bUseSimpleAverage5 == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.SIMPLE_AVG;
			f.numHistory = 5;		// Number of values to consider in mean
			fm.AddFilter (f);
		}

		if(m_bDblMovingAverage == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.DOUBLE_MOVING_AVG;
			f.numHistory = 5;		// Amount of history to keep
			fm.AddFilter (f);
		}

		if(m_bUseExpSmoothing == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.EXP_SMOOTHING;
			f.numHistory = 5;		// Amount of history to keep
			fm.AddFilter (f);
		}

		if(m_bUseDblExpSmoothing == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.DOUBLE_EXP_SMOOTHING;
			f.numHistory = 10;		
			fm.AddFilter (f);
		}

		if(m_bUseAdaptive == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.ADAPTIVE_DOUBLE_EXP_SMOOTHING;
			fm.AddFilter (f);
		}
		
		if(m_bUseMedian == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.MEDIAN;
			f.numHistory = 10;		// Amount of history to keep
			fm.AddFilter (f);
		}
		
		if(m_bUseCombination1 == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.COMBINATION1;
			fm.AddFilter (f);
		}
		
		if(m_bUseCombination2 == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.COMBINATION2;
			fm.AddFilter (f);
		}

		if(m_bUseNone == true) {
			Filter f = new Filter();
			f.name = Filter.FILTER_NAME.NONE;
			fm.AddFilter (f);
		}
		
		
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
		
		GUI.Box (new Rect(10, 25, 200, Screen.height - 10) , "Filter Menu");

		m_bUseSimpleAverage = GUI.Toggle(new Rect(45, 50,200,30), m_bUseSimpleAverage, " SIMPLE AVERAGE 10");

		m_bUseMovingAverage = GUI.Toggle(new Rect(45,90,200,30), m_bUseMovingAverage, " MOVING AVERAGE");

		m_bUseSimpleAverage5 = GUI.Toggle(new Rect(45,130,200,30), m_bUseSimpleAverage5, " SIMPLE AVERAGE 5");

		m_bDblMovingAverage = GUI.Toggle(new Rect(45,170,200,30), m_bDblMovingAverage, " DOUBLE MOV AVERAGE");

		m_bUseExpSmoothing = GUI.Toggle(new Rect(45,210,200,30), m_bUseExpSmoothing, " EXP SMOOTHING");

		m_bUseDblExpSmoothing = GUI.Toggle(new Rect(45,250,200,30), m_bUseDblExpSmoothing, " DOUBLE EXP SMOOTHING");

		m_bUseAdaptive = GUI.Toggle(new Rect(45,290,200,30), m_bUseAdaptive, " ADAPTIVE DBL EXP");

		m_bUseMedian = GUI.Toggle(new Rect(45,330,200,30), m_bUseMedian, " MEDIAN");
		
		m_bUseCombination1 = GUI.Toggle(new Rect(45,370,200,30), m_bUseCombination1, " SIMPLE AVG + Median");
		
		m_bUseCombination2 = GUI.Toggle(new Rect(45,410,200,30), m_bUseCombination2, " DBL MOV AVG + Median");
		
		m_bUseNone = GUI.Toggle(new Rect(45,450,200,30), m_bUseNone, " NONE");
		
		
		if( GUI.Button(new Rect(35, Screen.height - 50,150,30), "Clear"))
		{
			WriteableAreaResize();
			fv.DrawCircle();
		}
		
		// If some button has been pressed OR this is the first exection
		if(GUI.changed) 
		{
			LoadFilters();
			
			if(fm.filters.Count == 1)
				m_principalCursor = 0;

		}					

		int count = 0;
		if(m_bUseSimpleAverage) 
		{
			GUI.Label (new Rect(15, 45, 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseMovingAverage) 
		{
			GUI.Label (new Rect(15, 85 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}

		if(m_bUseSimpleAverage5) 
		{
			GUI.Label (new Rect(15, 125 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}

		if(m_bDblMovingAverage) 
		{
			GUI.Label (new Rect(15, 165 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}

		if(m_bUseExpSmoothing) 
		{
			GUI.Label (new Rect(15, 205 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseDblExpSmoothing) 
		{
			GUI.Label (new Rect(15, 245 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseAdaptive) 
		{
			GUI.Label (new Rect(15, 285 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseMedian) 
		{
			GUI.Label (new Rect(15, 325 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseCombination1) 
		{
			GUI.Label (new Rect(15, 365 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseCombination2) 
		{
			GUI.Label (new Rect(15, 405 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if(m_bUseNone) 
		{
			GUI.Label (new Rect(15, 445 , 30, 30), m_colorTextureDictionary[fm.colors[count].ToString ()]);
			count++;
		}
		
		if( prevScreenWidth != Screen.width || prevScreenHeight != Screen.height) 
		{
			// Resize writeable area, redraw the circle
			WriteableAreaResize();
			prevScreenWidth = Screen.width;
			prevScreenHeight = Screen.height;
			fv.DrawCircle();	
		}

		//AA: Draw the writeable area
		fv.Apply();
		for (int i = 0; i < fv.m_filterOutputTexture.Count ; i++) {
			GUI.DrawTexture(new Rect(fv.filterOutputLocX , fv.filterOutputLocY, fv.filterOutputWidth, fv.filterOutputHeight), fv.m_filterOutputTexture[i]);
		}
			
		
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
						fm.UpdateJointFilters(new Vector3(handX, handY, handZ), new Vector3(relX, relY, relZ));
						
						for(int i = 0 ; i< fm.filters.Count ; i++ )
						{
							if(m_bUseJointFiltering) {
								Debug.Log ("Prehand " + new Vector3(handX, handY, handZ) + " relJoint " + new Vector3(relX, relY, relZ));
								Vector3 handPos = fm.joints[i]; // filter.Update(new Vector3(handX, handY, handZ), Filter.JOINT_TYPE.JOINT);
								Vector3 relJointPos = fm.relativeJoints[i]; //filter.Update(new Vector3(relX, relY, relZ), Filter.JOINT_TYPE.RELATIVEJOINT);
								Debug.Log ("hand " + handPos + " relJoint " + relJointPos);
								handZ = handPos.z;
								handY = handPos.y;
								handX = handPos.x;
								
								relZ = relJointPos.z;
								relY = relJointPos.y;
								relX = relJointPos.x;
								m_relativeCursorPosition = fm.relativeCursorPosition[i];
								
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
									}
		
		                            // Apply the tracking to the current position with the given filter factor
									// AA: Filtering should happen here for joint-to-relativejoint (VECTOR) filtering
									// AA: filtering code
									
									Vector2 tempNew = new Vector2(newX,newY);
									
									fm.UpdateVectorFilters(m_relativeCursorPosition, tempNew, filterFactor);
									// If true, use the calculated factor for smoothing, else just use the new
									if(m_bUseVectorFiltering) {
										m_relativeCursorPosition =  fm.vectors[i]; //filter.Update(m_relativeCursorPosition, tempNew, filterFactor);
									}
									else {	// Just give equal weight to both
										m_relativeCursorPosition = filter.Update(m_relativeCursorPosition, tempNew, 0.5f);
									}
									
									// AA: Calculate all filters
									// fm.UpdateVectorFilters(m_relativeCursorPosition, tempNew, filterFactor);
									
		                            m_timeStamp = timeStamp;
		
		                            // Send it, but only if it is more or less within the screen
									if (m_relativeCursorPosition.x > -0.1f && m_relativeCursorPosition.x < 1.1f
										&& m_relativeCursorPosition.y > -0.1f && m_relativeCursorPosition.y < 1.1f)
									{
										
										
										MoveMouse(m_relativeCursorPosition.x, m_relativeCursorPosition.y, i);
										
										// Each filter must store it's own value of relative position, absolute and previous absolute positions
										fm.relativeCursorPosition[i] = m_relativeCursorPosition;
										fm.absPixelPosition[i] = m_absPixelPosition;
										fm.prevAbsPixelPosition[i] = m_previousAbsPixelPosition;

										DrawFilterOutputs(i);
										m_gotNewFubiCoordinates = true;
										m_lastCursorChangeDoneByFubi = true;
									}
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
						//Debug.Log ("m_mapping.x=" + m_mapping.x + "m_mapping.y=" + m_mapping.y + "m_mapping.height=" + m_mapping.height + "m_mapping.width=" + m_mapping.width);
						return true;
                    }
                }
            }
        }
		return false;
    }
	
	
	
}