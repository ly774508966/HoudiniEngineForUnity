/*
 * PROPRIETARY INFORMATION.  This software is proprietary to
 * Side Effects Software Inc., and is not to be reproduced,
 * transmitted, or disclosed in any way without written permission.
 *
 * Produced by:
 *      Side Effects Software Inc
 *      123 Front Street West, Suite 1401
 *      Toronto, Ontario
 *      Canada   M5J 2M2
 *      416-504-9876
 *
 * COMMENTS:
 *      Generated version information to be used when linking for sanity checks.
 */

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// WARNING! This file is GENERATED by Make.
// DO NOT modify manually or commit to the repository!
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Versions

public struct HoudiniVersion
{
	public const int HOUDINI_MAJOR			= 15;
	public const int HOUDINI_MINOR			= 0;
	public const int HOUDINI_BUILD			= 190;
	public const int HOUDINI_PATCH			= 0;

	public const int HOUDINI_ENGINE_MAJOR	= 2;
	public const int HOUDINI_ENGINE_MINOR	= 0;
	
	public const int HOUDINI_ENGINE_API		= 5;
	
#if UNITY_STANDALONE_WIN || ( UNITY_METRO && UNITY_EDITOR )
	public const string HAPI_LIBRARY		= "libHAPI";
#elif UNITY_STANDALONE_OSX
	public const string HAPI_LIBRARY		= "/Library/Frameworks/Houdini.framework/Versions/15.0.190/Houdini";
#else
	public const string HAPI_LIBRARY		= "libHAPI"; // Cannot be empty but its ok if not found.
#endif
};
