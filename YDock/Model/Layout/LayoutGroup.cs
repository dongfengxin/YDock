﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using YDock.Enum;
using YDock.Interface;
using YDock.View;

namespace YDock.Model
{
    public class LayoutGroup : BaseLayoutGroup
    {
        public LayoutGroup(DockSide side, DockMode mode, DockManager dockManager)
        {
            _side = side;
            _mode = mode;
            _dockManager = dockManager;
        }

        private AttachObject _attachObj;
        internal AttachObject AttachObj { get { return _attachObj; } set { _attachObj = value; } }

        protected override void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.OnChildrenCollectionChanged(sender, e);
            if (_view == null) return;
            var tab = _view as TabControl;
            if (e.NewItems?.Count > 0 && (e.NewItems[e.NewItems.Count - 1] as IDockElement).CanSelect)
                tab.SelectedIndex = IndexOf(e.NewItems[e.NewItems.Count - 1] as IDockElement);
            else tab.SelectedIndex = Math.Max(0, tab.SelectedIndex);
        }

        protected override void OnChildrenPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnChildrenPropertyChanged(sender, e);
            if (e.PropertyName == "CanSelect")
            {
                if (_view == null) return;
                if ((sender as DockElement).CanSelect)
                    (_view as TabControl).SelectedIndex = Children_CanSelect.Count() - 1;
                else (_view as TabControl).SelectedIndex = Children_CanSelect.Count() > 0 ? 0 : -1;
                if (Children_CanSelect.Count() == 0)
                    _DetachFromParent();
            }
        }

        private DockManager _dockManager;
        public override DockManager DockManager
        {
            get
            {
                return _dockManager;
            }
        }

        public override void SetActive(IDockElement element)
        {
            base.SetActive(element);
            if (_view != null)
                (_view as TabControl).SelectedIndex = IndexOf(element);
            else//_view不存在则要创建新的_view
            {
                if (_attachObj == null || !_attachObj.AttachTo())
                {
                    if (this is LayoutDocumentGroup)
                    {
                        var _children = Children.ToList();
                        _children.Reverse();
                        var dockManager = _dockManager;
                        Dispose();
                        foreach (var child in _children)
                            dockManager.Root.DocumentModels[0].Attach(child);
                    }
                    else
                    {
                        var ctrl = new AnchorSideGroupControl(this);
                        switch (Side)
                        {
                            case DockSide.Left:
                                _dockManager.LayoutRootPanel.RootGroupPanel.AttachChild(ctrl, AttachMode.Left, 0);
                                break;
                            case DockSide.Right:
                                _dockManager.LayoutRootPanel.RootGroupPanel.AttachChild(ctrl, AttachMode.Right, _dockManager.LayoutRootPanel.RootGroupPanel.Count);
                                break;
                            case DockSide.Top:
                                _dockManager.LayoutRootPanel.RootGroupPanel.AttachChild(ctrl, AttachMode.Top, 0);
                                break;
                            case DockSide.Bottom:
                                _dockManager.LayoutRootPanel.RootGroupPanel.AttachChild(ctrl, AttachMode.Bottom, _dockManager.LayoutRootPanel.RootGroupPanel.Count);
                                break;
                        }
                    }
                }
            }
        }

        public override void Detach(IDockElement element)
        {
            base.Detach(element);
            //保存Size信息
            if (_view != null)
            {
                (element as DockElement).DesiredHeight = (_view as BaseGroupControl).ActualHeight;
                (element as DockElement).DesiredWidth = (_view as BaseGroupControl).ActualWidth;
                //如果Children_CanSelect数量为0，且Container不是LayoutDocumentGroup，则尝试将view从界面移除
                if (Children_CanSelect.Count() == 0) //如果Children_CanSelect数量为0
                    _DetachFromParent();
            }
        }

        public override void Attach(IDockElement element, int index = -1)
        {
            if (!element.Side.Assert())
                throw new ArgumentException("Side is illegal!");
            base.Attach(element, index);
        }

        private void _DetachFromParent()
        {
            if ((_view as ILayoutGroupControl).TryDeatchFromParent())
            {
                _view = null;
                if (_children.Count == 0)
                    Dispose();
            }
        }

        public override void Dispose()
        {
            _attachObj?.Dispose();
            _attachObj = null;
            if (_view != null)
                _dockManager.DragManager.OnDragStatusChanged -= (_view as BaseGroupControl).OnDragStatusChanged;
            base.Dispose();
            _dockManager = null;
        }
    }

    public class LayoutDocumentGroup : LayoutGroup
    {
        public LayoutDocumentGroup(DockMode mode, DockManager dockManager) : base(DockSide.None, mode, dockManager)
        {

        }

        protected override void OnChildrenPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnChildrenPropertyChanged(sender, e);
            if (e.PropertyName == "IsActive")
                IsActive = (sender as IDockElement).IsActive;
        }

        private bool _isActive = false;
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    RaisePropertyChanged("IsActive");
                }
            }
        }

        public IEnumerable<IDockElement> ChildrenSorted
        {
            get
            {
                var listSorted = Children_CanSelect.ToList();
                listSorted.Sort();
                return listSorted;
            }
        }

        public override void Attach(IDockElement element, int index = -1)
        {
            if (element.Side != DockSide.None)
                throw new ArgumentException("Side is illegal!");
            base.Attach(element, index);
            if (element.IsActive) IsActive = true;
        }

        public override void Detach(IDockElement element)
        {
            base.Detach(element);
            if (element.IsActive) IsActive = false;
        }
    }
}