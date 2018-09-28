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

        private int _progress;
        private const int _ticksPerFrame = 20;
        public Texture2D Icon
        {
            get
            {
                switch ( Status )
                {
                    case WWWStatus.Downloading:
                        return Resources.Spinner[ _progress++ / _ticksPerFrame % Resources.Spinner.Length];
                    case WWWStatus.Error:
                        return Widgets.CheckboxOffTex;
                    default:
                        return Resources.Warning;
                }
            }
        }

        public Color Color
        {
            get
            {
                switch ( Status )
                {
                    case WWWStatus.Error:
                        return Color.red;
                    case WWWStatus.NotImplemented:
                        return Color.grey;
                    default:
                        return Color.white;
                }
            }
        }

        public void Update()
        {
            if ( finished )
                return;
            if ( www == null )
            {
                Debug.Log( "manifestUri not implemented, quitting." );
                _status = WWWStatus.NotImplemented;
                finished = true;
                return;
            }
            if ( !www.isDone )
            {
                Debug.Log("downloading...");
                _status = WWWStatus.Downloading;
            }
            if ( !www.error.NullOrEmpty() )
            {
                Debug.Log( $"error: {www.error}" );
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
                    manifest.SetVersion( false );
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