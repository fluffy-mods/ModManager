// VersionCheck.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Net;
using RimWorld;
using UnityEngine;
using Verse;
using Version = System.Version;

namespace ModManager
{
    public class VersionCheck: Dependency
    {
        private Version version;
        private bool completed = false;
        private bool downloading;
        private Exception exception;

        public VersionCheck( Manifest parent ) : base( parent, parent.Mod.PackageId )
        {
            // start crunching those downloads
            FetchManifest( parent.manifestUri );
        }

        public override int Severity => version == null || version <= parent.Version ? 0 : 1;

        public override string Tooltip
        {
            get
            {
                if ( exception != null )
                    return exception.Message;
                if ( downloading )
                    return I18n.DownloadPending;
                if ( IsSatisfied )
                    return I18n.LatestVersion;
                return I18n.NewVersionAvailable( parent.Version, version ?? new Version() );
            }
        }

        public override Color Color {
            get
            {
                if ( exception != null )
                    return Color.yellow;
                if ( downloading )
                    return Color.white;
                if ( IsSatisfied )
                    return Color.green;
                return GenUI.MouseoverColor;
            }
        }

        private static int TICKS_PER_FRAME = 10;
        private int frame;
        public override Texture2D StatusIcon
        {
            get
            {
                if ( exception != null )
                    return Resources.Warning;
                if ( downloading )
                {
                    var index = frame++ % TICKS_PER_FRAME;
                    return Resources.Spinner[index % Resources.Spinner.Length];
                }
                if ( IsSatisfied )
                    return Resources.Check;
                return Resources.Status_Up;
            }
        }

        public async void FetchManifest( string manifestUri )
        {
            var client = new WebClient();
            try
            {
                downloading = true;
                var      raw            = await client.DownloadStringTaskAsync( manifestUri );
                Manifest onlineManifest = DirectXmlLoader.ItemFromXmlString<Manifest>( raw, manifestUri );
                version = onlineManifest.Version;
            }
            catch ( WebException ex )
            {
                exception = ex;
                if ( Prefs.DevMode )
                    Log.Warning( $"Failed to fetch {manifestUri} ({ex.Status}:\n{ex.Message}" );
            }
            catch ( Exception ex )
            {
                exception = ex;
                Log.Error( $"Exception fetching {manifestUri}:\n{ex}"  );
            }
            finally
            {
                downloading = false;
                client.Dispose();
            }
        }

        public override void OnClicked( Page_ModsConfig window )
        {
            // do stuff.
        }

        public override bool IsSatisfied => completed && !downloading && exception == null && version <= parent.Version;

        public override string RequirementTypeLabel => "VersionCheck".Translate();
    }
}