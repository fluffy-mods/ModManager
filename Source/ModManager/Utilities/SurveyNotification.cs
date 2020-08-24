// SurveyNotification.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using UnityEngine;
using Verse;

namespace ModManager
{
    public static class SurveyNotification
    {
        public static void HandleNotification()
        {
            if ( ModManager.Settings.SurveyNotificationShown ) return;

            var msg = $"Hi there!\n\n" +
                      $"Thank you for using Mod Manager. As you know, I try hard to make my mods the best they can be.\n\n" +
                      $"Today, I need your help in determining what future development of Mod Manager should focus on. What new features would you most like to see?\n\n" +
                      $"I'm going to ask you to fill out a quick survey, it will take just 5 minutes of your time.\n\n" +
                      $"Thank you!\n\nCheers,\n   Fluffy";
            var title = $"I need YOU!";
            Action surveyAction = () =>
            {
                Application.OpenURL( "https://forms.gle/kdoSBUX2hpj69Zzq5" );
                ModManager.Settings.SurveyNotificationShown = true;
                ModManager.Settings.Write();
            };
            Action dismissAction = () =>
            {
                ModManager.Settings.SurveyNotificationShown = true;
                ModManager.Settings.Write();
            };
            var dialog = new Dialog_MessageBox( msg, "Open survey", surveyAction, "No, never", dismissAction, title, acceptAction: surveyAction )
            {
                buttonCText = "Maybe later",
                buttonCClose = true
            };
            Find.WindowStack.Add( dialog );
        }
    }
}