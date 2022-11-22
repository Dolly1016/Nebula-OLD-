using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Module
{
    public class HelpContent
    {
        static public HelpContent rootContent;

        List<HelpContent>? subContent;
        public HelpContent? Parent { get; private set; }

        public Action<MetaScreen.MSDesigner>? ContentGenerator { get; set; }

        public HelpContent(HelpContent? parent)
        {
            Parent = parent;
            subContent = null;
            ContentGenerator = null;

            if (Parent != null)
            {
                if (Parent.subContent == null) subContent = new List<HelpContent>();
                Parent.subContent.Add(this);
            }
        } 

    }
}
