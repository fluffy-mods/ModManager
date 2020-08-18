// VersionCheck.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Net;
using RimWorld;
using UnityEngine;
using Verse;
using Version = System.Version;

namespace ModManager
{
    public class VersionCheck: Dependency
    {
        private Manifest onlineManifest;
        private bool completed;
        private bool downloading;
        private Exception exception;

        public VersionCheck( Manifest parent ) : base( parent, parent.Mod.PackageId )
        {
            // start crunching those downloads
            FetchManifest( parent.manifestUri );
        }

        public override int Severity {
            get
            {
                if ( Prefs.DevMode && exception != null )
                    return 3;
                if ( exception != null )
                    return 0;
                if ( downloading )
                    return 1;
                if ( IsSatisfied )
                    return 0;
                return 2;
            }
        }

        public override bool IsApplicable => parent.Mod.Active &&
                                             ( ModManager.Settings.ShowVersionChecksOnSteamMods ||
                                               parent.Mod.Source == ContentSource.ModsFolder );

        public override List<FloatMenuOption> Resolvers
        {
            get
            {

                var options = Utilities.NewOptionsList;
                if ( onlineManifest != null && !onlineManifest.downloadUri.NullOrEmpty() )
                    options.Add( new FloatMenuOption( I18n.OpenDownloadUri( onlineManifest.downloadUri ),
                                                      () => SteamUtility.OpenUrl( onlineManifest.downloadUri ) ) );
                else
                    options.Add( new FloatMenuOption( I18n.NoDownloadUri, null ) );
                return options;
            }
        }

        public override string Tooltip
        {
            get
            {
                if ( exception != null )
                    return I18n.FetchingOnlineManifestFailed( exception.Message );
                if ( downloading )
                    return I18n.DownloadPending;
                if ( IsSatisfied )
                    return I18n.LatestVersion;
                return I18n.NewVersionAvailable( parent.Version, onlineManifest.Version ?? new Version() );
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
                onlineManifest = DirectXmlLoader.ItemFromXmlString<Manifest>( raw, manifestUri );
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
                completed = true;
                client.Dispose();
            }
        }

        public override bool IsSatisfied => completed && !downloading && exception == null && onlineManifest.Version <= parent.Version;

        public override bool CheckSatisfied()
        {
            // do nothing.
            return true;
        }

        public override string RequirementTypeLabel => "VersionCheck".Translate();
    }
}