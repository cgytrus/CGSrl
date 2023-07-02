namespace Cgsrl.Shared.Environment;

public interface IInteractable {
    public string prompt { get; }
    public void Interact(PlayerObject player);
}
