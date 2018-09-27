// ModSync.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Xml;
using Verse;

namespace ModManager
{
    public class ModSync
    {
        private string identifier;
        private string version;

        public void LoadDataFromXmlCustom( XmlNode root )
        {
            foreach ( XmlNode node in root.ChildNodes )
            {
                switch ( node.Name )
                {
                    // we may care about some other fields, but screw that.
                    case "ID":
                        identifier = node.InnerText;
                        break;
                    case "Version":
                        version = node.InnerText;
                        break;
                }
            }    
        }

        public Manifest Manifest( ModMetaData mod )
        {
            return new Manifest( mod, version, identifier );
        }
    }
}