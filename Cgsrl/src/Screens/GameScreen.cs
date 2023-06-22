using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Cgsrl.Environment;
using Cgsrl.Screens.Templates;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class GameScreen : LayoutResource, IScreen {
    public const string GlobalId = "layouts/game";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type>();

    protected override IEnumerable<KeyValuePair<string, Type>> dependencyTypes {
        get {
            foreach(KeyValuePair<string, Type> pair in base.dependencyTypes)
                yield return pair;
            yield return new KeyValuePair<string, Type>(ResourcePackSelectorTemplate.GlobalId,
                typeof(ResourcePackSelectorTemplate));
        }
    }

    private readonly IResources _resources;
    private Level? _level;

    public GameScreen(IResources resources) => _resources = resources;

    public override void Load(string id) {
        base.Load(id);

        _level = new Level(renderer, input, audio, _resources);

        for(int y = -20; y <= 20; y++) {
            for(int x = -20; x <= 20; x++) {
                _level.Add(new FloorObject { position = new Vector2Int(x, y) });
            }
        }

        _level.Add(new PlayerObject());

        for(int i = -5; i <= 5; i++) {
            _level.Add(new WallObject { position = new Vector2Int(i, -5) });
        }
        for(int i = 0; i < 100; i++) {
            _level.Add(new WallObject { position = new Vector2Int(i, -8) });
        }
        for(int i = 0; i < 30; i++) {
            _level.Add(new WallObject { position = new Vector2Int(8, i - 7) });
        }

        _level.Add(new EffectObject {
            position = new Vector2Int(3, -10),
            size = new Vector2Int(10, 10),
            effect = renderer.formattingEffects["glitch"]
        });
        _level.Add(new EffectObject {
            position = new Vector2Int(-12, -24),
            size = new Vector2Int(6, 9),
            effect = renderer.formattingEffects["glitch"]
        });
    }

    public void Open() { }
    public void Close() { }

    public void Update(TimeSpan time) {
        _level?.Update(time);
        foreach((string _, Element element) in elements)
            element.Update(time);
        if(input.KeyPressed(KeyCode.Escape) &&
            Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen);
    }

    public void Tick(TimeSpan time) => _level?.Tick(time);
}
