using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace XVTwiddle.ViewModels.Pages
{
    /// <summary>
    /// Selects the <see cref="DataTemplate"/> for a page.
    /// </summary>
    public class PageTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// The <see cref="DataTemplate"/> for a <see cref="ProjectViewModel"/>.
        /// </summary>
        public DataTemplate ProjectPage { get; set; } = null!;

        /// <summary>
        /// Creates a new <see cref="PageTemplateSelector"/>.
        /// </summary>
        public PageTemplateSelector() { }

        /// <summary>
        /// Returns a <see cref="DataTemplate"/> based on the type of the object passed in.
        /// </summary>
        /// <param name="item">
        /// The item to switch against.
        /// </param>
        /// <param name="container">
        /// The container.
        /// </param>
        /// <returns>
        /// A <see cref="DataTemplate"/>.
        /// </returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container) => item switch
        {
            ProjectViewModel _ => this.ProjectPage,
            _ => base.SelectTemplate(item, container),
        };
    }
}