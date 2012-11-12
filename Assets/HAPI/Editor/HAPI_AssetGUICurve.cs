/*
 * PROPRIETARY INFORMATION.  This software is proprietary to
 * Side Effects Software Inc., and is not to be reproduced,
 * transmitted, or disclosed in any way without written permission.
 *
 * Produced by:
 *      Side Effects Software Inc
 *		123 Front Street West, Suite 1401
 *		Toronto, Ontario
 *		Canada   M5J 2M2
 *		416-504-9876
 *
 * COMMENTS:
 * 
 */


using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using HAPI;

[ CustomEditor( typeof( HAPI_AssetCurve ) ) ]
public class HAPI_AssetGUICurve : HAPI_AssetGUI 
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public
	
	public override void OnEnable() 
	{
		base.OnEnable();
		
		myAssetCurve = target as HAPI_AssetCurve;
		
		myUnbuiltChanges = false;
		
		myCurrentlyActivePoint = -1;
		
		if ( GUI.changed )
			myAssetCurve.build();
	}
	
	public override void OnInspectorGUI() 
	{
		base.OnInspectorGUI();
		
		myLabelStyle = new GUIStyle( GUI.skin.label );
		myLabelStyle.alignment = TextAnchor.MiddleRight;
		
		bool isMouseUp = false;
		Event curr_event = Event.current;
		if ( curr_event.isMouse && curr_event.type == EventType.MouseUp )
			isMouseUp = true;
		
		bool commitChanges = false;
		if ( curr_event.isKey && curr_event.type == EventType.KeyUp && curr_event.keyCode == KeyCode.Return )
			commitChanges = true;
		
		///////////////////////////////////////////////////////////////////////
		// Draw Game Object Controls
		
		myAssetCurve.prShowObjectControls = 
			EditorGUILayout.Foldout( myAssetCurve.prShowObjectControls, new GUIContent( "Object Controls" ) );
		
		if ( myAssetCurve.prShowObjectControls ) 
		{	
			if ( GUILayout.Button( "Rebuild" ) ) 
			{
				myAssetCurve.prFullRebuild = true;
				myAssetCurve.build();
			}
			
			// Draw Auto Select Asset Node Toggle
			EditorGUILayout.BeginHorizontal(); 
			{
				// Add padding for the toggle column.
				EditorGUILayout.LabelField( myNullContent, myToggleWidthGUI );
				// Add empty space to align with fields.
				EditorGUILayout.LabelField( myNullContent, myLabelWidthGUI );
				
				// Draw toggle with its label.
				bool old_value = myAssetCurve.prAutoSelectAssetNode;
				myAssetCurve.prAutoSelectAssetNode = EditorGUILayout.Toggle( old_value, myToggleWidthGUI );
				EditorGUILayout.SelectableLabel( "Auto Select Parent", myLineHeightGUI );
			}
			EditorGUILayout.EndHorizontal();
			
			// Draw Logging Toggle
			EditorGUILayout.BeginHorizontal(); 
			{
				// Add padding for the toggle column.
				EditorGUILayout.LabelField( myNullContent, myToggleWidthGUI );
				// Add empty space to align with fields.
				EditorGUILayout.LabelField( myNullContent, myLabelWidthGUI );
				
				// Draw toggle with its label.
				bool old_value = myAssetCurve.prEnableLogging;
				myAssetCurve.prEnableLogging = EditorGUILayout.Toggle( old_value, myToggleWidthGUI );
				EditorGUILayout.SelectableLabel( "Enable Logging", myLineHeightGUI );
			}
			EditorGUILayout.EndHorizontal();
			
		} // if
		
		///////////////////////////////////////////////////////////////////////
		// Draw Asset Controls
		
		EditorGUILayout.Separator();
		myAssetCurve.prShowAssetControls = 
			EditorGUILayout.Foldout( myAssetCurve.prShowAssetControls, new GUIContent( "Asset Controls" ) );
		
		bool hasAssetChanged = false;
		myDelayBuild = false;
		if ( myAssetCurve.prShowAssetControls )
		{
			if ( GUILayout.Button( "Add Point" ) )
				myAssetCurve.addPoint( Vector3.zero );
			hasAssetChanged |= generateAssetControls();
		}
		
		if ( ( hasAssetChanged && !myDelayBuild ) || ( myUnbuiltChanges && commitChanges ) )
		{
			myAssetCurve.build();
			myUnbuiltChanges = false;
		}
		else if ( hasAssetChanged )
			myUnbuiltChanges = true;
		
		if ( isMouseUp || commitChanges )
		{
			try
			{
				int bufLength = 0;
				HAPI_Host.getPreset( myAssetCurve.prAssetId, myAssetCurve.prPreset, ref bufLength );
				
				myAssetCurve.prPreset = new byte[ bufLength ];
				
				HAPI_Host.getPreset( myAssetCurve.prAssetId, myAssetCurve.prPreset, ref bufLength );
			}
			catch {} // Just catch them here but don't report them because we would just get a huge stream of errors.
		}
	}
	
	public void OnSceneGUI() 
	{
		Event current_event 		= Event.current;
		
		int point_count 			= myAssetCurve.prPoints.Count;
		int pressed_point_index 	= -1;
		Vector3 previous_position 	= Vector3.zero;
		
		Vector3[] vertices = myAssetCurve.prVertices;
		for ( int i = 0; vertices != null && i < vertices.Length; ++i )
		{
			Vector3 position = vertices[ i ];
			
			if ( i == 0 )
			{
				previous_position = position;
				continue;
			}
			
			Handles.color = Color.grey;
			Handles.DrawLine( previous_position, position );
			previous_position = position;
		}
			
		for ( int i = 0; i < point_count; ++i ) 
		{
			Vector3 position 	= myAssetCurve.prPoints[ i ];
			float handle_size 	= HandleUtility.GetHandleSize( position ) * 0.2f;
			
			Handles.color 		= Color.cyan;
			bool buttonPress 	= Handles.Button( 	position, 
													Quaternion.LookRotation( Camera.current.transform.position ),
													handle_size,
													handle_size,
													Handles.CircleCap );
			
			if ( buttonPress )
				pressed_point_index = i;
			
			Handles.Label( position, new GUIContent( "p" + i ) );
			
			previous_position = position;
		}
		
		if ( pressed_point_index >= 0 )
			myCurrentlyActivePoint = pressed_point_index;
		
		if ( myCurrentlyActivePoint >= 0 ) 
		{
			Vector3 old_position = myAssetCurve.prPoints[ myCurrentlyActivePoint ];
			Vector3 new_position = Handles.PositionHandle( old_position, 
														   Quaternion.identity );
			
			if ( new_position != old_position )
				myAssetCurve.updatePoint( myCurrentlyActivePoint, new_position );
		}
		
		if ( current_event.isKey && current_event.keyCode == KeyCode.Escape )
			myCurrentlyActivePoint = -1;
	}
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private
	
	/// <summary>
	/// 	Creates two empty label fields, gets the rectangles from each, and combines it to create
	/// 	the last double rectangle. This is used for <see cref="GUI.HorizontalSlider"/> which
	/// 	uses absolute positioning and needs a rectangle to know it's size and position.
	/// 	This way, we can insert sliders within the relative positioning of the Inspector GUI elements.
	/// </summary>
	/// <returns>
	/// 	The last double rectangle.
	/// </returns>
	private Rect getLastDoubleRect()
	{		
		// Draw first empty label field. 
		EditorGUILayout.LabelField( myNullContent, myDummyLabelMinWidthGUI );
		float xMin = GUILayoutUtility.GetLastRect().xMin;
		float yMin = GUILayoutUtility.GetLastRect().yMin;
		float width = GUILayoutUtility.GetLastRect().width;
		float height = GUILayoutUtility.GetLastRect().height;
		
		// Draw second empty label field.
		EditorGUILayout.LabelField( myNullContent, myDummyLabelMinWidthGUI );
		float width2 = GUILayoutUtility.GetLastRect().width;
		
		// Create the double rectangle from the two above.
		Rect last_double_rect = new Rect( xMin, yMin, width + width2, height );
		
		return last_double_rect;
	}
	
	/// <summary>
	/// 	Draws a single asset control.
	/// </summary>
	/// <param name="id">
	/// 	Corresponding parameter id as given by <see cref="HAPI_Host.GetParameters"/>.
	/// </param>
	/// <param name="join_last">
	/// 	Determines if the current control should be put on the same line as the previous one.
	/// 	Also serves as a return value to be used with the next control.
	/// </param>
	/// <param name="no_label_toggle_last">
	/// 	Determines if the current control should not have its label drawn.
	/// 	Also serves as a return value to be used with the next control.
	/// </param>
	/// <returns>
	/// 	<c>true</c> if the parameter value corresponding to this control has changed, <c>false</c> otherwise.
	/// </returns>
	private bool generateAssetControl( 	int id, ref bool join_last, ref bool no_label_toggle_last ) 
	{
		if ( myAssetCurve.prParms == null )
			return false;
		
		bool changed 				= false;
		
		int asset_id				= myAssetCurve.prAssetId;
		
		HAPI_ParmInfo[] parms 		= myAssetCurve.prParms;
		HAPI_ParmInfo parm			= parms[ id ];
		
		int[] parm_int_values		= myAssetCurve.prParmIntValues;
		float[] parm_float_values	= myAssetCurve.prParmFloatValues;
		int[] parm_string_values	= myAssetCurve.prParmStringValues;
		
		HAPI_ParmType parm_type 	= (HAPI_ParmType) parm.type;
		int parm_size				= parm.size;
		
		int values_index = -1;
		if ( parm.isInt() )
		{
			if ( parm.intValuesIndex < 0 || parm_int_values == null )
				return false;
			values_index 			= parm.intValuesIndex;
		}
		else if ( parm.isFloat() )
		{
			if ( parm.floatValuesIndex < 0 || parm_float_values == null )
				return false;
			values_index			= parm.floatValuesIndex;
		}
		else if ( parms[ id ].isString() )
		{
			if ( parm.stringValuesIndex < 0 || parm_string_values == null )
				return false;
			values_index			= parm.stringValuesIndex;
		}
		
		GUIStyle slider_style 		= new GUIStyle( GUI.skin.horizontalSlider );
		GUIStyle slider_thumb_style	= new GUIStyle( GUI.skin.horizontalSliderThumb );
				
		if ( parms[ id ].invisible )
			return changed;
						
		// Decide whether to join with the previous parameter on the same 
		// line or not.
		if ( !join_last || parm_size > 1 )
			EditorGUILayout.BeginHorizontal();
		
		// Add label first if we're not a toggle.
		if ( parm_type != HAPI_ParmType.HAPI_PARMTYPE_TOGGLE
			&& parm_type != HAPI_ParmType.HAPI_PARMTYPE_FOLDER
			&& !parm.labelNone )
		{
			GUILayoutOption label_final_width = myLabelWidthGUI;
			if ( join_last && !no_label_toggle_last )
			{
				float min_width;
				float max_width;
				myLabelStyle.CalcMinMaxWidth( new GUIContent( parm.label ), out min_width, out max_width );
				label_final_width = GUILayout.Width( min_width );
			}
			else if ( !join_last )
			{
				// Add padding for the toggle column.
				EditorGUILayout.LabelField( "", myToggleWidthGUI );
			}
			EditorGUILayout.SelectableLabel( parm.label, myLabelStyle, label_final_width, myLineHeightGUI );
			no_label_toggle_last = false;
		}
		
		///////////////////////////////////////////////////////////////////////
		// Integer Parameter
		if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_INT )
		{
			if ( parm.choiceCount > 0 && parm.choiceIndex >= 0 )
			{
				// Draw popup (menu) field.
				List< string > 	labels = new List< string >();
				List< int>		values = new List< int >();
				
				// Go through our choices.
				for ( int i = 0; i < parm.choiceCount; ++i )
				{
					if ( myAssetCurve.prParmChoiceLists[ parm.choiceIndex + i ].parentParmId != id )
						Debug.LogError( "Parm choice parent parm id (" 
										+ myAssetCurve.prParmChoiceLists[ parm.choiceIndex + i ].parentParmId 
										+ ") not matching current parm id (" + id + ")!\n"
										+ "Choice index: " + ( parm.choiceIndex + i ) + ", "
										+ "Choice count: " + parm.choiceCount );
					
					labels.Add( myAssetCurve.prParmChoiceLists[ parm.choiceIndex + i ].label );
					values.Add( i );
				}
				
				// Get old value.
				int old_value = parm_int_values[ values_index ];
				
				// Draw popup.
				int new_value = EditorGUILayout.IntPopup( old_value, labels.ToArray(), values.ToArray() );
				
				// Determine if value changed and update parameter value.
				if ( new_value != old_value )
				{
					parm_int_values[ values_index ] = new_value;
					changed |= true;
				}
			}
			else
			{
				int per_line = 0;
				for ( int p = 0; p < parm_size; ++p, ++per_line )
				{
					if ( per_line >= myMaxFieldCountPerLine )
					{
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField( "", myToggleWidthGUI );
						EditorGUILayout.LabelField( "", myLabelWidthGUI );
						per_line = 0;
					}
					
					// Get old value.
					int old_value = parm_int_values[ values_index + p ];
					
					// Draw field.
					int new_value = EditorGUILayout.IntField( old_value );
					if ( new_value != old_value ) // Check if the field is being used instead of the slider.
						myDelayBuild = true;
					
					// Draw the slider.
					if ( parm_size == 1 && !join_last && !parm.joinNext )
					{
						float ui_min = ( parm.hasUIMin ? parm.UIMin : 0.0f );
						float ui_max = ( parm.hasUIMax ? parm.UIMax : 10.0f );
						Rect lastDoubleRect = getLastDoubleRect();
						slider_style.stretchWidth = false;
						slider_style.fixedWidth = lastDoubleRect.width;
						new_value = (int) GUI.HorizontalSlider( lastDoubleRect, new_value, ui_min, ui_max, 
																slider_style, slider_thumb_style );
					}
					
					// Enforce min/max bounds.
					if ( parm.hasMin && new_value < (int) parm.min )
						new_value = (int) parm.min;
					if ( parm.hasMax && new_value > (int) parm.max )
						new_value = (int) parm.max;
					
					// Determine if value changed and update parameter value.
					if ( new_value != old_value )
					{
						parm_int_values[ values_index + p ] = new_value;
						changed |= true;
					} // if
				} // for
			} // if parm.choiceCount
		} // if parm_type is INT
		///////////////////////////////////////////////////////////////////////
		// Float Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FLOAT )
		{
			int per_line = 0;
			for ( int p = 0; p < parm_size; ++p, ++per_line )
			{
				if ( per_line >= myMaxFieldCountPerLine )
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField( "", myToggleWidthGUI );
					EditorGUILayout.LabelField( "", myLabelWidthGUI );
					per_line = 0;
				}
				
				// Get old value.
				float old_value = parm_float_values[ values_index + p ];
				
				// Draw field.
				float new_value = EditorGUILayout.FloatField( old_value );
				if ( new_value != old_value ) // Check if the field is being used instead of the slider.
					myDelayBuild = true;
				
				// Draw the slider.
				if ( parm_size == 1 && !join_last && !parm.joinNext )
				{
					float ui_min = ( parm.hasUIMin ? parm.UIMin : 0.0f );
					float ui_max = ( parm.hasUIMax ? parm.UIMax : 10.0f );
					Rect lastDoubleRect = getLastDoubleRect();
					slider_style.stretchWidth = false;
					slider_style.fixedWidth = lastDoubleRect.width;
					new_value = GUI.HorizontalSlider( lastDoubleRect, new_value, ui_min, ui_max, 
													  slider_style, slider_thumb_style );
				}
				
				// Enforce min/max bounds.
				if ( parm.hasMin && new_value < parm.min )
					new_value = parm.min;
				if ( parm.hasMax && new_value > parm.max )
					new_value = parm.max;
				
				// Determine if value changed and update parameter value.
				if ( new_value != old_value )
				{
					parm_float_values[ values_index + p ] = new_value;
					changed |= true;
				} // if
			} // for
		} // if parm_type is FLOAT
		///////////////////////////////////////////////////////////////////////
		// String Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_STRING )
		{			
			int per_line = 0;
			for ( int p = 0; p < parm_size; ++p, ++per_line )
			{
				if ( per_line >= myMaxFieldCountPerLine )
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField( "", myToggleWidthGUI );
					EditorGUILayout.LabelField( "", myLabelWidthGUI );
					per_line = 0;
				}
				
				// Get old value.
				string old_value = HAPI_Host.getString( parm_string_values[ values_index + p ] );
				
				// Draw field.
				string new_value = EditorGUILayout.TextField( old_value );
				if ( new_value != old_value ) // Check if the field is being used instead of the slider.
					myDelayBuild = true;
				
				// Determine if value changed and update parameter value. 
				if ( new_value != old_value )
				{
					HAPI_Host.setParmStringValue( asset_id, new_value, id, p );
					changed |= true;
				}
			}
		}
		///////////////////////////////////////////////////////////////////////
		// File Field
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FILE )
		{
			string old_path = HAPI_Host.getString( parm_string_values[ values_index ] );
			string new_path = EditorGUILayout.TextField( old_path );
			if ( new_path != old_path ) // Check if the field is being used instead of the slider.
				myDelayBuild = true;
			
			if ( GUILayout.Button( "...", GUILayout.Width( myFileChooserButtonWidth ) ) ) 
			{
				string prompt_path = EditorUtility.OpenFilePanel( "Select File", old_path, "*" );;
				if ( prompt_path.Length > 0 )
					new_path = prompt_path;
			}
			
			if ( new_path != old_path )
			{
				HAPI_Host.setParmStringValue( asset_id, new_path, id, 0 );
				changed |= true;
			}
		}
		///////////////////////////////////////////////////////////////////////
		// Toggle Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_TOGGLE )
		{
			if ( !parm.joinNext )
			{
				// Add padding for the toggle column.
				EditorGUILayout.LabelField( myNullContent, myToggleWidthGUI );
				// Add empty space to align with fields.
				EditorGUILayout.LabelField( myNullContent, myLabelWidthGUI );
			}
			
			// Get old value.
			int old_value = parm_int_values[ values_index ];
			
			// Draw toggle with its label.
			bool toggle_result = EditorGUILayout.Toggle( old_value != 0, myToggleWidthGUI );
			int new_value = ( toggle_result ? 1 : 0 );
			if ( !parms[ id ].labelNone )
				EditorGUILayout.SelectableLabel( parms[ id ].label, myLineHeightGUI );
			else
				no_label_toggle_last = true;
			
			// Determine if value changed and update parameter value.
			if ( new_value != old_value )
			{
				parm_int_values[ values_index ] = new_value;
				changed |= true;
			}
		}		
		///////////////////////////////////////////////////////////////////////
		// Color Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_COLOUR )
		{
			Color old_color = new Color( parm_float_values[ values_index + 0 ], 
									 	 parm_float_values[ values_index + 1 ], 
									 	 parm_float_values[ values_index + 2 ] );
			if ( parm_size > 3 )
				old_color.a = parm_float_values[ values_index + 3 ];
			
			// Draw control.
			Color new_color = EditorGUILayout.ColorField( old_color );
			
			// Determine if value changed and update parameter value.
			if ( new_color != old_color )
			{
				parm_float_values[ values_index + 0 ] = new_color.r;
				parm_float_values[ values_index + 1 ] = new_color.g;
				parm_float_values[ values_index + 2 ] = new_color.b;
				
				if ( parm_size > 3 )
					parm_float_values[ values_index + 3 ] = new_color.a;
			
				changed |= true;
			}
		}		
		///////////////////////////////////////////////////////////////////////
		// Separator
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_SEPARATOR )
		{
			EditorGUILayout.Separator();
		}
		
		// Decide whether to join with the next parameter on the same line or not
		// but also save our status for the next parameter.
		join_last = ( parm.joinNext && parm_size <= 1 );
		if ( !parm.joinNext || parm_size > 1 )
			EditorGUILayout.EndHorizontal();
		
		if ( myAssetCurve.hasProgressBarBeenUsed() && id == myAssetCurve.prLastChangedParmId )
		{
			// TODO: Set the focus back to this control since the progress bar would have stolen it.	
		}
		
		if ( changed )
		{
			myAssetCurve.prLastChangedParmId = id;
		
			if ( parm.isInt() )
			{
				int[] temp_int_values = new int[ parm_size ];
				for ( int p = 0; p < parm_size; ++p )
					temp_int_values[ p ] = parm_int_values[ values_index + p ];
				HAPI_Host.setParmIntValues( asset_id, temp_int_values, values_index, parm_size );
			}
			else if ( parm.isFloat() )
			{
				float[] temp_float_values = new float[ parm_size ];
				for ( int p = 0; p < parm_size; ++p )
					temp_float_values[ p ] = parm_float_values[ values_index + p ];
				HAPI_Host.setParmFloatValues( asset_id, temp_float_values, values_index, parm_size );
			}
			
			// Note: String parameters update their values themselves so no need to do anything here.
		}
		
		return changed;
	}
	
	/// <summary>
	/// 	Draws all asset controls.
	/// </summary>
	/// <returns>
	/// 	<c>true</c> if any of the control values have changed from the corresponding cached parameter
	/// 	values, <c>false</c> otherwise.
	/// </returns>
	private bool generateAssetControls() 
	{
		if ( myAssetCurve.prParms == null )
			return false;
		
		bool changed 					= false;
		int current_index 				= 0;
		HAPI_ParmInfo[] parms 			= myAssetCurve.prParms;
				
		bool join_last 					= false;
		bool no_label_toggle_last 		= false;
		
		int folder_list_count 			= 0;
		
		// These stacks maintain the current folder depth, parent id, and how many more child 
		// parameters are still contained in the current folder.
		Stack< int > parent_id_stack 		= new Stack< int >();
		Stack< int > parent_count_stack 	= new Stack< int >();
		
		
		// Loop through all the parameters.
		while ( current_index < myAssetCurve.prParmCount )
		{
			int current_parent_id = -1; // The root has parent id -1.
			
			// If we're not at the root (empty parent stack), get the current parent id and parent 
			// count from the stack as well as decrement the parent count as we're about to parse 
			// another parameter.
			if ( parent_id_stack.Count != 0 )
		    {
				current_parent_id = parent_id_stack.Peek();
				
				if ( parent_count_stack.Count == 0 ) Debug.LogError( "" );
				
				// If the current parameter, whatever it may be, does not belong to the current active
				// parent then skip it. This check has to be done here because we do not want to
				// increment the top of the parent_count_stack as if we included a parameter in the
				// current folder.
				if ( parms[ current_index ].parentId != current_parent_id )
				{
					current_index++;
					continue;
				}				
				
				int current_parent_count = parent_count_stack.Peek();
				current_parent_count--;
				
				// If we've reached the last parameter in the current folder we need to pop the parent 
				// stacks (we're done with the current folder). Otherwise, update the top of the 
				// parent_count_stack.
				if ( current_parent_count <= 0 )
				{
					parent_id_stack.Pop();
					parent_count_stack.Pop();
				}
				else
				{
					parent_count_stack.Pop();
					parent_count_stack.Push( current_parent_count );
				}
		    }
			else if ( parms[ current_index ].parentId != current_parent_id )
			{
				// If the current parameter does not belong to the current active parent then skip it.
				current_index++;
				continue;
			}
			
			HAPI_ParmType parm_type = (HAPI_ParmType) parms[ current_index ].type;
			
			if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FOLDERLIST )
			{
				// The current parameter is a folder list which means the next parms[ current_index ].size
				// parameters will be folders belonging to this folder list. Push to the stack a new
				// folder depth, ready to eat the next few parameters belonging to the folder list's 
				// selected folder.
				
				bool folder_list_invisible	= parms[ current_index ].invisible;
				int folder_count 			= parms[ current_index ].size;
				int first_folder_index 		= current_index + 1;
				int last_folder_index 		= current_index + folder_count;
				
				// Generate the list of folders which will be passed to the GUILayout.Toolbar() method.
				List< int > 	tab_ids 	= new List< int >();
				List< string > 	tab_labels 	= new List< string >();
				List< int > 	tab_sizes 	= new List< int >();
				bool has_visible_folders	= false;
				for ( current_index = first_folder_index; current_index <= last_folder_index; ++current_index )
				{
					if ( parms[ current_index ].type != (int) HAPI_ParmType.HAPI_PARMTYPE_FOLDER )
					{
						Debug.LogError( "We should be iterating through folders only here!"
							+ "\nCurrent Index: " + current_index + ", folder_count: " + folder_count );
					}
					
					// Don't add this folder if it's invisible.
					if ( parms[ current_index ].invisible || folder_list_invisible )
						continue;
					else
						has_visible_folders = true;
					
					tab_ids.Add( 		parms[ current_index ].id );
					tab_labels.Add( 	parms[ current_index ].label );
					tab_sizes.Add( 		parms[ current_index ].size );
				}
				current_index--; // We decrement the current_index as we incremented one too many in the for loop.
				
				// If there are no folders visible in this folder list, don't even append the folder stacks.
				if ( has_visible_folders )
				{
					folder_list_count++;
					
					// If myObjectControl.myFolderListSelections is smaller than our current depth it means this
					// is the first GUI generation for this asset (no previous folder selection data) so
					// increase the size of the selection arrays to accomodate the new depth.
					if ( myAssetCurve.prFolderListSelections.Count <= folder_list_count )
					{
						myAssetCurve.prFolderListSelections.Add( 0 );
						myAssetCurve.prFolderListSelectionIds.Add( -1 );
					}
					
					int selected_folder 	= myAssetCurve.prFolderListSelections[ folder_list_count ];
					selected_folder 		= GUILayout.Toolbar( selected_folder, tab_labels.ToArray() );
					myAssetCurve.prFolderListSelections[ folder_list_count ] = selected_folder;
					
					// Push only the selected folder info to the parent stacks since for this depth and this folder
					// list only the parameters of the selected folder need to be generated.
					parent_id_stack.Push( 		tab_ids[ selected_folder ] );
					parent_count_stack.Push( 	tab_sizes[ selected_folder ] );
				}
			}
			else
			{
				// The current parameter is a simple parameter so just draw it.
				
				if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FOLDER )
					Debug.LogError( "All folders should have been parsed in the folder list if clause!" );
				
				changed |= generateAssetControl( current_index, ref join_last, ref no_label_toggle_last );
			}
			
			current_index++;
		}
				
		return changed;
	}

	private HAPI_AssetCurve	myAssetCurve;
	private bool			myDelayBuild;
	private bool			myUnbuiltChanges;
	
	private int 			myCurrentlyActivePoint;
}