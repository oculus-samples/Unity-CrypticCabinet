// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Interfaces;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    public static class Telemetry
    {
        public const string META_HUB_CONTEXT = "metahub";
        public const string TUTORIAL_HUB_CONTEXT = "tuthub";
        private const string EVENT_PAGE_LOADED = "pageload";
        private const string EVENT_NAV = "nav";
        private const string EVENT_WINDOW_CLOSED = "closed";
        private const string EVENT_WINDOW_OPEN = "open";
        private const string EVENT_SHOW_ON_STARTUP_TOGGLED = "sost";
        private const string EVENT_DYN_REFERENCE = "dynref";
        private const string EVENT_SCENE_OPEN = "scene_open";
        private const string EVENT_IMAGE_CLICKED = "img_clicked";
        private const string EVENT_OPEN_TUTORIAL_BTN_CLICKED = "open_btn_clicked";

        public static void OnPageLoaded(string telemetryContext, IPageInfo page)
        {
            SendEvent(telemetryContext, EVENT_PAGE_LOADED, $"{page.ProjectName}_{page.Name}");
        }

        public static void OnNavigation(string telemetryContext, string selectedPageId)
        {
            SendEvent(telemetryContext, EVENT_NAV, selectedPageId);
        }

        public static void OnWindowClosed(string telemetryContext, string selectedPageId)
        {
            SendEvent(telemetryContext, EVENT_WINDOW_CLOSED, selectedPageId);
        }

        public static void OnWindowOpen(string telemetryContext, string selectedPageId)
        {
            SendEvent(telemetryContext, EVENT_WINDOW_OPEN, selectedPageId);
        }

        public static void OnShowOnStartupToggled(string telemetryContext, bool newVal, string projectName)
        {
            SendEvent(telemetryContext, EVENT_SHOW_ON_STARTUP_TOGGLED,
                (newVal ? "1" : "0") + $"|{projectName}");
        }

        public static void OnHighlightDynamicReference(string telemetryContext, string objectName, IPageInfo page)
        {
            SendEvent(
                telemetryContext, EVENT_DYN_REFERENCE, $"{objectName}|{page.Name}|{page.ProjectName}");
        }

        public static void OnSceneOpen(string telemetryContext, string sceneName, IPageInfo page)
        {
            SendEvent(
                telemetryContext, EVENT_SCENE_OPEN, $"{sceneName}|{page.Name}|{page.ProjectName}");
        }

        public static void OnImageClicked(string telemetryContext, IPageInfo page, string imageName)
        {
            SendEvent(
                telemetryContext, EVENT_IMAGE_CLICKED, $"{imageName}|{page.Name}|{page.ProjectName}");
        }


        public static void OnOpenTutorialButton(string contextTitle)
        {
            SendEvent(
                TUTORIAL_HUB_CONTEXT, EVENT_OPEN_TUTORIAL_BTN_CLICKED, contextTitle);
        }

        private static string BuildEventName(string telemetryContext, string eventName)
        {
            return $"{telemetryContext}_{eventName}";
        }

        private static void SendEvent(string telemetryContext, string eventName, string param)
        {
            var name = BuildEventName(telemetryContext, eventName);
            param = SanitizeParam(param);
            _ = OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
            _ = OVRPlugin.SendEvent(name, param, "integration");
            // Debug.Log($"Send Event: {name} | {param}");
        }

        private static string SanitizeParam(string evt)
        {
            // remove whitespace
            return evt.Replace(" ", "");
        }
    }
}