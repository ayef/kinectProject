using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AA: This class handles to all the visualizations for the filter
public class FilterVisualization {
	
	public Texture2D m_baseScreen;	// Display a shape for the user to follow
	public List<Texture2D> m_filterOutputTexture;	// Texture used for drawing the filter outputs
	public int [] filterOutputIndices;		// Used in case of multiple filters
	public int filterOutputLocX = Screen.width/3;	// Coordinates of top left corner of filter output texture
	public int filterOutputLocY = 10;					// 
	public int filterOutputWidth;	// Width and height of the kinect-writeable area
	public int filterOutputHeight;	// Initialized in Start()
	public int borderWidth = 10;	// Control witdh of border on all sides on right & bottom side of writeable area
	
	private int numPens = 5;		// Number of pens for drawing filters with (should be dynamic
	
	Color [][] pens;				// Colors for colouring the different filter outputs

	public void SetPixels(Vector2 absPosition, int filterNumber) 
	{
		
		m_filterOutputTexture[filterNumber].SetPixels((int)( absPosition.x), (int)((filterOutputHeight - absPosition.y) ), 2, 2, pens[filterNumber] );
			//m_filterOutputTexture[i].SetPixels((int)( absPosition.x), (int)((filterOutputHeight - absPosition.y) ), 2, 2, pens[1] );
		//m_filterOutputTexture.SetPixels((int)( absPosition.x), (int)((filterOutputHeight - absPosition.y) ), 2, 2, pens[1] );
		
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
		
		pens = new Color[numPens][];
		
		for (int i = 0; i < numPens; i++) {
			pens[i] = new Color[4] ;
			pens[i][0] = pens[i][1] = pens[i][2] = pens[i][3] = colors[i];
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
		DrawCircle(m_filterOutputTexture[0], m_filterOutputTexture[0].width/2, m_filterOutputTexture[0].height/2, (int)(m_filterOutputTexture[0].width*0.25f), Color.yellow);
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
	
	
	// AA: Code for drawing lines from: http://wiki.unity3d.com/index.php?title=DrawLine
	// http://wiki.unity3d.com/index.php?title=TextureDrawLine
	private void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
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
