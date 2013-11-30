using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AA: This class handles to all the visualizations for the filter
public class FilterVisualization {
	
	public Texture2D m_baseScreen;	// Display a shape for the user to follow
	public List<Texture2D> m_filterOutputTexture;	// Texture used for drawing the filter outputs note: To improve performance, just draw everything on one texture
	public int [] filterOutputIndices;		// Used in case of multiple filters
	public int filterOutputLocX = Screen.width/3;	// Coordinates of top left corner of filter output texture
	public int filterOutputLocY = 10;					// 
	public int filterOutputWidth;	// Width and height of the kinect-writeable area
	public int filterOutputHeight;	// Initialized in Start()
	public int borderWidth = 10;	// Control witdh of border on all sides on right & bottom side of writeable area
	
	private int numPens = 7;		// Number of pens for drawing filters with (should be dynamic
	
	Color [][] pens;				// Colors for colouring the different filter outputs
	int lineWidth = 4;
	int penWidth = 4;
	Dictionary<string, int> penColors;
	
	public void SetPixels(Vector2 absPosition, Color col) 
	{
		Debug.Log("col.ToString(): " + col.ToString());
		m_filterOutputTexture[0].SetPixels((int)( absPosition.x), (int)((filterOutputHeight - absPosition.y) ), penWidth, penWidth, pens[penColors[col.ToString()]] );
	}
	
	public void DrawLine(int i, int prevAbsPixelPositionx, int prevAbsPixelPositiony, int absPixelPositionx, int absPixelPositiony, Color col) 
	{
		if(prevAbsPixelPositionx != 0 || prevAbsPixelPositiony != 0 )
			DrawLine(m_filterOutputTexture[i], prevAbsPixelPositionx, ( (filterOutputHeight - prevAbsPixelPositiony) ), absPixelPositionx, ( (filterOutputHeight - absPixelPositiony) ), col);
		//m_filterOutputTexture[filterNumber].SetPixels((int)( absPosition.x), (int)((filterOutputHeight - absPosition.y) ), 2, 2, pens[filterNumber] );
		
	}
	
	// Add txtures for displaying each filter
	public void AddFilterDisplayTexture()
	{
		Texture2D layer = new Texture2D(filterOutputWidth , filterOutputHeight);
		Color [] transparency = new Color [ filterOutputWidth * filterOutputHeight];
		Color c = new Color(0.2F, 0.3F, 0.4F, 0.5F);
		for(int i = 0; i< filterOutputWidth * filterOutputHeight; i++) {
			transparency[i] = Color.clear;
		}
		layer.SetPixels(transparency);
		m_filterOutputTexture.Add(layer);
	}
	
	// Remove all of the filter display layers except the base layer
	public void Clear() 
	{
		for (int i = 1; i < m_filterOutputTexture.Count ; i++) {
				m_filterOutputTexture.RemoveAt(i);
		}
	}
	public FilterVisualization () 
	{
		Color[] colors = new Color[numPens];
		colors[0] = Color.green;
		colors[1] = Color.red;
		colors[2] = Color.blue;
		colors[3] = Color.black;
		colors[4] = Color.magenta;
		colors[5] = Color.cyan;
		colors[6] = Color.yellow;
		
		pens = new Color[numPens][];
		penColors = new Dictionary<string, int>();
		
		for (int i = 0; i < numPens; i++) {
			pens[i] = new Color[penWidth*penWidth] ;
			for (int j = 0; j < penWidth*penWidth; j++) {
				pens[i][j] = colors[i];
			}
			penColors.Add(colors[i].ToString (), i);
		}

		m_filterOutputTexture = new List<Texture2D> ();
		m_baseScreen = new Texture2D(filterOutputWidth , filterOutputHeight);
		m_filterOutputTexture.Add(m_baseScreen);
	}

	//AA: 
	public void Initialise() 
	{
		filterOutputWidth = Screen.width - filterOutputLocX - borderWidth ;
		filterOutputHeight = Screen.height - filterOutputLocY - borderWidth;

		Color [] transparency = new Color [ filterOutputWidth * filterOutputHeight];
		Color c = new Color(0.2F, 0.3F, 0.4F, 0.5F);
		for(int i = 0; i< filterOutputWidth * filterOutputHeight; i++) {
			transparency[i] = Color.clear;
		}
		
		for (int i = 0; i < m_filterOutputTexture.Count ; i++) {
			if( m_filterOutputTexture[i] == null)
				m_filterOutputTexture[i] = new Texture2D(filterOutputWidth , filterOutputHeight);
			else
				m_filterOutputTexture[i].Resize (filterOutputWidth , filterOutputHeight);
			
			if(i>0)
				m_filterOutputTexture[i].SetPixels(transparency);
		}
	}
	
	public void DrawCircle()
	{
		DrawCircle(m_filterOutputTexture[0], m_filterOutputTexture[0].width/2, m_filterOutputTexture[0].height/2, (int)(m_filterOutputTexture[0].width*0.25f), Color.green, lineWidth);
	}
	
	// AA: circle code from http://wiki.unity3d.com/index.php?title=TextureDrawCircle
	// Draws circle at cx, cy, on the passed texture
	private static void DrawCircle (Texture2D tex, int cx, int cy, int r, Color col) {

		int y = r;
		float d = 1/4 - r;
		float end = Mathf.Ceil(r/Mathf.Sqrt(2));
	 
		for (int x = 0; x <= end; x++) {
			tex.SetPixel(cx+x, cy+y, col);
			tex.SetPixel(cx+x, cy-y, col);
			tex.SetPixel(cx-x, cy+y, col);
			tex.SetPixel(cx-x, cy-y, col);
			tex.SetPixel(cx+y, cy+x, col);
			tex.SetPixel(cx-y, cy+x, col);
			tex.SetPixel(cx+y, cy-x, col);
			tex.SetPixel(cx-y, cy-x, col);
	 
			d += 2*x+1;
			if (d > 0) {
				d += 2 - 2*y--;
			}
		}
	}	
	
	private static void DrawCircle (Texture2D tex, int cx, int cy, int r, Color col, int width) {

		int y = r;
		float d = 1/4 - r;
		float end = Mathf.Ceil(r/Mathf.Sqrt(2));
	 	Color [] thick = new Color[width*width];
		for(int i = 0; i< width*width ;i++) {
			thick[i] = col;
		}

		for (int x = 0; x <= end; x++) {
			tex.SetPixels(cx+x, cy+y, width, width, thick);
			tex.SetPixels(cx+x, cy-y, width, width, thick);
			tex.SetPixels(cx-x, cy+y, width, width, thick);
			tex.SetPixels(cx-x, cy-y, width, width, thick);
			tex.SetPixels(cx+y, cy+x, width, width, thick);
			tex.SetPixels(cx-y, cy+x, width, width, thick);
			tex.SetPixels(cx+y, cy-x, width, width, thick);
			tex.SetPixels(cx-y, cy-x, width, width, thick);
//			tex.SetPixel(cx+x, cy+y, col);
//			tex.SetPixel(cx+x, cy-y, col);
//			tex.SetPixel(cx-x, cy+y, col);
//			tex.SetPixel(cx-x, cy-y, col);
//			tex.SetPixel(cx+y, cy+x, col);
//			tex.SetPixel(cx-y, cy+x, col);
//			tex.SetPixel(cx+y, cy-x, col);
//			tex.SetPixel(cx-y, cy-x, col);
	 
			d += 2*x+1;
			if (d > 0) {
				d += 2 - 2*y--;
			}
		}
	}
	
	
	// AA: Code for drawing lines from: http://wiki.unity3d.com/index.php?title=DrawLine
	// http://wiki.unity3d.com/index.php?title=TextureDrawLine
	public void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
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
	
	// Apply changes to filter texture
	public void Apply () {
		for(int i = 0; i < m_filterOutputTexture.Count; i++) {
			m_filterOutputTexture[i].Apply();
		}
	}
	
}
