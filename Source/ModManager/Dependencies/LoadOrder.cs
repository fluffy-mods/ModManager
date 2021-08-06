// LoadOrder.cs
// Copyright Karel Kroeze, -2020

using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Verse;
using static ModManager.Utilities;

namespace ModManager {

    public abstract class LoadOrder: Dependency {
        protected LoadOrder(Manifest parent, string packageId) : base(parent, packageId) {
        }

        protected LoadOrder(Manifest parent, ModDependency _depend) : base(parent, _depend) {
        }

        public override bool IsApplicable => (parent?.Mod?.Active ?? false) && (Target?.Active ?? false);

        public override Color Color => IsSatisfied ? Color.white : Color.red;

        public override int Severity => IsSatisfied ? 0 : 3;
    }

    public class LoadOrder_Before: LoadOrder {
        public LoadOrder_Before() : base(null, string.Empty) { }

        public LoadOrder_Before(Manifest parent, string packageId) : base(parent, packageId) { }

        public override List<FloatMenuOption> Resolvers {
            get {
                List<FloatMenuOption> options = NewOptionsList;
                options.Add(new FloatMenuOption(I18n.MoveBefore(parent.Button, Target.GetManifest().Button),
                                                  () => ModButtonManager.MoveBefore(
                                                      parent.Button, Target.GetManifest().Button)));
                options.Add(new FloatMenuOption(I18n.MoveAfter(Target.GetManifest().Button, parent.Button),
                                                  () => ModButtonManager.MoveAfter(
                                                      Target.GetManifest().Button, parent.Button)));
                return options;
            }
        }

        public override string Tooltip {
            get {
                if (!IsApplicable) {
                    return "Not applicable";
                }

                return IsSatisfied
                    ? I18n.LoadedBefore(Target.Name)
                    : I18n.ShouldBeLoadedBefore(Target.Name);
            }
        }

        public override bool CheckSatisfied() {
            List<ModMetaData> mods = ModButtonManager.ActiveMods;
            return Target != null && Target.Active && parent.Mod.Active && mods.IndexOf(Target) > mods.IndexOf(parent.Mod);
        }

        public override string RequirementTypeLabel => "loadOrder".Translate();

        public void LoadDataFromXmlCustom(XmlNode root) {
            string text = root.InnerText.Trim();
            TryParseIdentifier(text, root);
        }

    }

    public class LoadOrder_After: LoadOrder {
        public LoadOrder_After() : base(null, string.Empty) { }
        public LoadOrder_After(Manifest parent, string packageId) : base(parent, packageId) { }

        public override List<FloatMenuOption> Resolvers {
            get {
                List<FloatMenuOption> options = NewOptionsList;
                options.Add(new FloatMenuOption(I18n.MoveAfter(parent.Button, Target.GetManifest().Button),
                                                  () => ModButtonManager.MoveAfter(
                                                      parent.Button, Target.GetManifest().Button)));
                options.Add(new FloatMenuOption(I18n.MoveBefore(Target.GetManifest().Button, parent.Button),
                                                  () => ModButtonManager.MoveBefore(
                                                      Target.GetManifest().Button, parent.Button)));
                return options;
            }
        }

        public override string Tooltip {
            get {
                if (!IsApplicable) {
                    return "Not applicable";
                }

                return IsSatisfied
                    ? I18n.LoadedAfter(Target.Name)
                    : I18n.ShouldBeLoadedAfter(Target.Name);
            }
        }

        public override bool CheckSatisfied() {
            List<ModMetaData> mods = ModButtonManager.ActiveMods;
            return Target != null &&
                   Target.Active &&
                   parent.Mod.Active &&
                   mods.IndexOf(Target) < mods.IndexOf(parent.Mod);
        }

        public override string RequirementTypeLabel => "loadOrder".Translate();

        public void LoadDataFromXmlCustom(XmlNode root) {
            string text = root.InnerText.Trim();
            TryParseIdentifier(text, root);
        }
    }
}
