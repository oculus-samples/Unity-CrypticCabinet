// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Contexts;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    public class TutorialPageGroup : PageGroup
    {
        public string TutorialContext { get; }

        public TutorialPageGroup(MetaHubContext context, Action<PageReference> onDrawPage) : base(context, onDrawPage) { }

        public TutorialPageGroup(BaseTutorialHubContext context, Action<PageReference> onDrawPage) : base(context, onDrawPage)
            => TutorialContext = context.TutorialName;
    }
}