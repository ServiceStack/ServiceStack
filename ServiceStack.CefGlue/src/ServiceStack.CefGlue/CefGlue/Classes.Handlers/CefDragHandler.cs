namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to dragging. The methods of
    /// this class will be called on the UI thread.
    /// </summary>
    public abstract unsafe partial class CefDragHandler
    {
        private int on_drag_enter(cef_drag_handler_t* self, cef_browser_t* browser, cef_drag_data_t* dragData, CefDragOperationsMask mask)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_dragData = CefDragData.FromNative(dragData);
            var m_result = OnDragEnter(m_browser, m_dragData, mask);

            return m_result ? 1 : 0;
        }

        /// <summary>
        /// Called when an external drag event enters the browser window. |dragData|
        /// contains the drag event data and |mask| represents the type of drag
        /// operation. Return false for default drag handling behavior or true to
        /// cancel the drag event.
        /// </summary>
        protected abstract bool OnDragEnter(CefBrowser browser, CefDragData dragData, CefDragOperationsMask mask);


        private static readonly CefDraggableRegion[] EmptyDraggableRegion = new CefDraggableRegion[0];

        private void on_draggable_regions_changed(cef_drag_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, UIntPtr regionsCount, cef_draggable_region_t* regions)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            CefDraggableRegion[] m_regions;
            var m_count = (int)regionsCount;
            if (m_count == 0) m_regions = EmptyDraggableRegion;
            else
            {
                m_regions = new CefDraggableRegion[m_count];
                for (var i = 0; i < m_count; i++)
                {
                    m_regions[i] = CefDraggableRegion.FromNative(regions + i);
                }
            }

            OnDraggableRegionsChanged(m_browser, m_frame, m_regions);
        }

        /// <summary>
        /// Called whenever draggable regions for the browser window change. These can
        /// be specified using the '-webkit-app-region: drag/no-drag' CSS-property. If
        /// draggable regions are never defined in a document this method will also
        /// never be called. If the last draggable region is removed from a document
        /// this method will be called with an empty vector.
        /// </summary>
        protected abstract void OnDraggableRegionsChanged(CefBrowser browser, CefFrame frame, CefDraggableRegion[] regions);
    }
}
