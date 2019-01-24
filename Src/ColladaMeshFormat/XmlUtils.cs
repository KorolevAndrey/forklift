// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace GeneralMeshUtils
{
	public static class XmlUtils
	{
		public static XmlNode GetRootNode( XmlNode node )
		{
			if( node.NodeType != XmlNodeType.Document )
				return GetRootNode( node.OwnerDocument );

			foreach( XmlNode childNode in node.ChildNodes )
			{
				if( childNode.NodeType != XmlNodeType.XmlDeclaration &&
					childNode.NodeType != XmlNodeType.ProcessingInstruction )
				{
					return childNode;
				}
			}

			return node;
		}

		public static XmlNode LoadFromText( string xmlText )
		{
			XmlDocument document = new XmlDocument();
			document.LoadXml( xmlText );
			return GetRootNode( document );
		}

		public static XmlNode FindChildNode( XmlNode node, string childName )
		{
			foreach( XmlNode childNode in node.ChildNodes )
				if( childNode.Name == childName )
					return childNode;
			return null;
		}

		public static string GetAttribute( XmlNode node, string attributeName )
		{
			if( node.Attributes != null )
			{
				XmlAttribute attribute = node.Attributes[ attributeName ];
				if( attribute != null && attribute.Value != null )
					return attribute.Value;
			}
			return "";
		}

	}
}
