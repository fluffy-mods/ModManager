// Manifest.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace ModManager
{
    

    public class OnlineManifest
    {
        public Manifest manifest;
        public string error;

        private WWW www;
        private WWWStatus _status = WWWStatus.NotImplemented;
        private bool finished;

        public OnlineManifest( string url )
        {
            if ( !url.NullOrEmpty() )
                www = new WWW( url );
        }

        public WWWStatus Status
        {
            get
            {
                Update();
                return _status;
            }
        }

        public Texture2D Icon
        {
            get
            {
                switch ( Status )
                {
                    case WWWStatus.Done:
                        return Widgets.CheckboxOnTex;
                    case WWWStatus.Downloading:
                        return Widgets.CheckboxPartialTex;
                    case WWWStatus.Error:
                        return Widgets.CheckboxOffTex;
                    default:
                        return Resources.Warning;
                }
            }
        }

        public void Update()
        {
            if ( finished )
                return;
            if ( www == null )
            {
                _status = WWWStatus.NotImplemented;
                finished = true;
                return;
            }
            if ( !www.isDone )
            {
                _status = WWWStatus.Downloading;
            }
            if ( !www.error.NullOrEmpty() )
            {
                _status = WWWStatus.Error;
                error = www.error;
                finished = true;
                return;
            }
            if ( www.isDone )
            {
                Debug.Log( $"Fetching {www.url} completed: {www.text}" );
                try
                {
                    manifest = IO.ItemFromXmlString<Manifest>( www.text );
                    _status = WWWStatus.Done;
                }
                catch ( Exception e )
                {
                    _status = WWWStatus.Error;
                    error = e.Message;
                }
                finished = true;
            }
        }
        
        public enum WWWStatus
        {
            NotImplemented,
            Error,
            Downloading,
            Done
        }
    }
}