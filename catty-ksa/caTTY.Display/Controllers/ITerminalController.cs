using System.Text;

namespace caTTY.Display.Controllers;

/// <summary>
///     Interface for a terminal controller that handles ImGui display and input.
///     This controller bridges the headless terminal emulator with ImGui rendering.
/// </summary>
public interface ITerminalController : IDisposable
{
    /// <summary>
    ///     Gets or sets whether the terminal window is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    ///     Gets whether the terminal window currently has focus.
    /// </summary>
    bool HasFocus { get; }

    /// <summary>
    ///     Updates the controller state. Should be called each frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds</param>
    void Update(float deltaTime);

    /// <summary>
    ///     Renders the terminal ImGui window and handles input.
    ///     Should be called during the ImGui render phase.
    /// </summary>
    void Render();

    /// <summary>
    ///     Event raised when user input should be sent to the process.
    ///     The string contains the encoded bytes/escape sequences to send.
    /// </summary>
    event EventHandler<DataInputEventArgs>? DataInput;
}

/// <summary>
///     Event arguments for data input from the terminal controller.
/// </summary>
public class DataInputEventArgs : EventArgs
{
    /// <summary>
    ///     Creates new data input event arguments.
    /// </summary>
    /// <param name="data">The input data as a string</param>
    /// <param name="bytes">The input data as raw bytes</param>
    public DataInputEventArgs(string data, byte[] bytes)
    {
        Data = data;
        Bytes = bytes;
    }

    /// <summary>
    ///     Creates new data input event arguments from a string.
    /// </summary>
    /// <param name="data">The input data as a string</param>
    public DataInputEventArgs(string data) : this(data, Encoding.UTF8.GetBytes(data))
    {
    }

    /// <summary>
    ///     Creates new data input event arguments from bytes.
    /// </summary>
    /// <param name="bytes">The input data as raw bytes</param>
    public DataInputEventArgs(byte[] bytes) : this(Encoding.UTF8.GetString(bytes), bytes)
    {
    }

    /// <summary>
    ///     The input data as a string (may contain escape sequences).
    /// </summary>
    public string Data { get; }

    /// <summary>
    ///     The input data as raw bytes.
    /// </summary>
    public byte[] Bytes { get; }
}
