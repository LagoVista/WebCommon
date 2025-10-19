// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 303f4a8270690b75a6664d605ea1e9db6a164e6acbb762de77f5fe2be7f4e302
// IndexVersion: 0
// --- END CODE INDEX META ---
using System.Globalization;
using System.Reflection;

//Resources:CommonResources:Common_Add
namespace LagoVista.IoT.Web.Common.Resources
{
	public class CommonResources
	{
        private static global::System.Resources.ResourceManager _resourceManager;
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static global::System.Resources.ResourceManager ResourceManager 
		{
            get 
			{
                if (object.ReferenceEquals(_resourceManager, null)) 
				{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LagoVista.IoT.Web.Common.Resources.CommonResources", typeof(CommonResources).GetTypeInfo().Assembly);
                    _resourceManager = temp;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        ///   Returns the formatted resource string.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static string GetResourceString(string key, params string[] tokens)
		{
			var culture = CultureInfo.CurrentCulture;;
            var str = ResourceManager.GetString(key, culture);

			for(int i = 0; i < tokens.Length; i += 2)
				str = str.Replace(tokens[i], tokens[i+1]);
										
            return str;
        }
        
        /// <summary>
        ///   Returns the formatted resource string.
        /// </summary>
		/*
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static HtmlString GetResourceHtmlString(string key, params string[] tokens)
		{
			var str = GetResourceString(key, tokens);
							
			if(str.StartsWith("HTML:"))
				str = str.Substring(5);

			return new HtmlString(str);
        }*/
		
		public static string Common_Add { get { return GetResourceString("Common_Add"); } }
//Resources:CommonResources:Common_AppName

		public static string Common_AppName { get { return GetResourceString("Common_AppName"); } }
//Resources:CommonResources:Common_Back

		public static string Common_Back { get { return GetResourceString("Common_Back"); } }
//Resources:CommonResources:Common_Cancel

		public static string Common_Cancel { get { return GetResourceString("Common_Cancel"); } }
//Resources:CommonResources:Common_Change

		public static string Common_Change { get { return GetResourceString("Common_Change"); } }
//Resources:CommonResources:Common_Disable


		///<summary>
		///(Verb version)
		///</summary>
		public static string Common_Disable { get { return GetResourceString("Common_Disable"); } }
//Resources:CommonResources:Common_Disabled


		///<summary>
		///(Current Status Version)
		///</summary>
		public static string Common_Disabled { get { return GetResourceString("Common_Disabled"); } }
//Resources:CommonResources:Common_Done

		public static string Common_Done { get { return GetResourceString("Common_Done"); } }
//Resources:CommonResources:Common_Enable


		///<summary>
		///Verb
		///</summary>
		public static string Common_Enable { get { return GetResourceString("Common_Enable"); } }
//Resources:CommonResources:Common_Enabled


		///<summary>
		///(Current Status Version)
		///</summary>
		public static string Common_Enabled { get { return GetResourceString("Common_Enabled"); } }
//Resources:CommonResources:Common_Login

		public static string Common_Login { get { return GetResourceString("Common_Login"); } }
//Resources:CommonResources:Common_Manage

		public static string Common_Manage { get { return GetResourceString("Common_Manage"); } }
//Resources:CommonResources:Common_Next

		public static string Common_Next { get { return GetResourceString("Common_Next"); } }
//Resources:CommonResources:Common_None

		public static string Common_None { get { return GetResourceString("Common_None"); } }
//Resources:CommonResources:Common_Password

		public static string Common_Password { get { return GetResourceString("Common_Password"); } }
//Resources:CommonResources:Common_Remove

		public static string Common_Remove { get { return GetResourceString("Common_Remove"); } }
//Resources:CommonResources:Common_Save

		public static string Common_Save { get { return GetResourceString("Common_Save"); } }
//Resources:CommonResources:Common_Submit

		public static string Common_Submit { get { return GetResourceString("Common_Submit"); } }
//Resources:CommonResources:Validation_Common_InvalidEmailAddress


		///<summary>
		///[FIELDNAME] should not be localized
		///</summary>
		public static string Validation_Common_InvalidEmailAddress { get { return GetResourceString("Validation_Common_InvalidEmailAddress"); } }
//Resources:CommonResources:Validation_Common_IsRequired


		///<summary>
		///[FIELDNAME] should not be localized and be used to populate the name of the field
		///</summary>
		public static string Validation_Common_IsRequired { get { return GetResourceString("Validation_Common_IsRequired"); } }
//Resources:CommonResources:Validation_Common_StringLength_Max


		///<summary>
		///[FIELDNAME],  [MAXLENGTH] should not be localized, will be used to populate values
		///</summary>
		public static string Validation_Common_StringLength_Max { get { return GetResourceString("Validation_Common_StringLength_Max"); } }
//Resources:CommonResources:Validation_Common_StringLength_Min


		///<summary>
		///[FIELDNAME], [MINLENGTH] should not be localized, will be used to populate values
		///</summary>
		public static string Validation_Common_StringLength_Min { get { return GetResourceString("Validation_Common_StringLength_Min"); } }
//Resources:CommonResources:Validation_Common_StringLength_MinMax


		///<summary>
		///[FIELDNAME], [MINLENGTH] and [MAXLENGTH] should not be localized, will be used to populate values
		///</summary>
		public static string Validation_Common_StringLength_MinMax { get { return GetResourceString("Validation_Common_StringLength_MinMax"); } }
//Resources:CommonResources:Validation_RegEx_Namespace

		public static string Validation_RegEx_Namespace { get { return GetResourceString("Validation_RegEx_Namespace"); } }

		public static class Names
		{
			public const string Common_Add = "Common_Add";
			public const string Common_AppName = "Common_AppName";
			public const string Common_Back = "Common_Back";
			public const string Common_Cancel = "Common_Cancel";
			public const string Common_Change = "Common_Change";
			public const string Common_Disable = "Common_Disable";
			public const string Common_Disabled = "Common_Disabled";
			public const string Common_Done = "Common_Done";
			public const string Common_Enable = "Common_Enable";
			public const string Common_Enabled = "Common_Enabled";
			public const string Common_Login = "Common_Login";
			public const string Common_Manage = "Common_Manage";
			public const string Common_Next = "Common_Next";
			public const string Common_None = "Common_None";
			public const string Common_Password = "Common_Password";
			public const string Common_Remove = "Common_Remove";
			public const string Common_Save = "Common_Save";
			public const string Common_Submit = "Common_Submit";
			public const string Validation_Common_InvalidEmailAddress = "Validation_Common_InvalidEmailAddress";
			public const string Validation_Common_IsRequired = "Validation_Common_IsRequired";
			public const string Validation_Common_StringLength_Max = "Validation_Common_StringLength_Max";
			public const string Validation_Common_StringLength_Min = "Validation_Common_StringLength_Min";
			public const string Validation_Common_StringLength_MinMax = "Validation_Common_StringLength_MinMax";
			public const string Validation_RegEx_Namespace = "Validation_RegEx_Namespace";
		}
	}
}
