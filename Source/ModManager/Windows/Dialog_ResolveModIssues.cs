// Dialog_ResolveModIssues.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using Verse;

namespace ModManager
{
    public class Dialog_ResolveModIssues: Dialog_MessageBox
    {
        public Dialog_ResolveModIssues( 
            string text, 
            string buttonAText = null, 
            Action buttonAAction = null, 
            string buttonBText = null, 
            Action buttonBAction = null, 
            string title = null, 
            bool buttonADestructive = false, 
            Action acceptAction = null, 
            Action cancelAction = null ) : base( text, buttonAText, buttonAAction, buttonBText, buttonBAction, title, buttonADestructive, acceptAction, cancelAction )
        {

        }
    }
}