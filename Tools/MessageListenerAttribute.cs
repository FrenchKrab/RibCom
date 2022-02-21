using System;



namespace RibCom.Tools
{	
	[System.AttributeUsage(System.AttributeTargets.Method)]
	public class MessageListener : System.Attribute  
	{  
		//public Type[] ListenedTypes {get; private set;}
	
	//params Type[] type
		public MessageListener()
		{  
			//ListenedTypes = type;
		}  
	}  
}