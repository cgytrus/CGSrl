﻿using System.Collections.Generic;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens.Templates;

public class ResourcePackSelectorTemplate(
    SettingsScreen screen,
    IList<ResourcePackData> availablePacks,
    ISet<ResourcePackData> loadedPacks)
    : ListBoxTemplateResource<ResourcePackData> {
    public const string GlobalId = "layouts/templates/resourcePackItem";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    private readonly SettingsScreen _screen = screen;
    private readonly IList<ResourcePackData> _availablePacks = availablePacks;
    private readonly ISet<ResourcePackData> _loadedPacks = loadedPacks;

    public override void Preload() {
        base.Preload();
        AddLayout("resourcePackItem");
        AddElement<Button>("toggle");
        AddElement<Button>("up");
        AddElement<Button>("down");
    }

    private class Template : BasicTemplate {
        private readonly ResourcePackSelectorTemplate _resource;
        private int _index;
        private ResourcePackData _item;
        private bool _loaded;

        public Template(ResourcePackSelectorTemplate resource) : base(resource) {
            _resource = resource;

            Button toggleButton = GetElement<Button>("toggle");
            toggleButton.onClick += (_, _) => {
                bool canUnload = _resource._loadedPacks.Count > 1 &&
                    _item.name != Core.engine.resources.defaultPackName;
                if(!canUnload && _loaded)
                    return;

                if(_loaded)
                    _resource._loadedPacks.Remove(_item);
                else
                    _resource._loadedPacks.Add(_item);
                _resource._screen.UpdatePacks();
            };
            toggleButton.onHover += (_, _) => {
                _resource._screen.UpdatePackDescription(_item.meta.description);
            };

            GetElement<Button>("up").onClick += (_, _) => {
                _resource._availablePacks.RemoveAt(_index);
                _resource._availablePacks.Insert(_index + 1, _item);
                _resource._screen.UpdatePacks();
            };

            GetElement<Button>("down").onClick += (_, _) => {
                _resource._availablePacks.RemoveAt(_index);
                _resource._availablePacks.Insert(_index - 1, _item);
                _resource._screen.UpdatePacks();
            };
        }

        public override void UpdateWithItem(int index, ResourcePackData item, int width) {
            _index = index;
            _item = item;

            int maxY = _resource._availablePacks.Count - 1;
            int y = maxY - index;

            _loaded = _resource._loadedPacks.Contains(item);

            bool canMoveUp = y > 0 && item.name != Core.engine.resources.defaultPackName;
            bool canMoveDown = y < maxY &&
                _resource._availablePacks[index - 1].name != Core.engine.resources.defaultPackName;

            Button toggleButton = GetElement<Button>("toggle");
            toggleButton.text =
                item.name.Length > toggleButton.size.x ? item.name[..toggleButton.size.x] : item.name;
            toggleButton.toggled = _loaded;

            Button moveUpButton = GetElement<Button>("up");
            moveUpButton.active = canMoveUp;

            Button moveDownButton = GetElement<Button>("down");
            moveDownButton.active = canMoveDown;
        }

        public override void MoveTo(Vector2Int origin, int index, Vector2Int size) {
            int maxY = _resource._availablePacks.Count - 1;
            int y = maxY - index;
            y *= height;
            foreach((string id, Element element) in idElements)
                element.position = _resource.GetElement(id).position + origin + new Vector2Int(0, y);
        }
    }

    public override IListBoxTemplateFactory<ResourcePackData>.Template CreateTemplate() => new Template(this);
}
