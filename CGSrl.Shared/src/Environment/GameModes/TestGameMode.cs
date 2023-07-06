namespace CGSrl.Shared.Environment.GameModes;

public class TestGameMode : SandboxGameMode {
    protected override void Initialize() {
        level.doLighting = false;
        base.Initialize();
    }
}
