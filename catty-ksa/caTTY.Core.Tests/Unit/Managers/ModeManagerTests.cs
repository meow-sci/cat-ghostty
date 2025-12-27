using caTTY.Core.Managers;
using NUnit.Framework;

namespace caTTY.Core.Tests.Unit.Managers;

/// <summary>
///     Unit tests for ModeManager class.
///     Tests terminal mode state tracking in isolation.
/// </summary>
[TestFixture]
public class ModeManagerTests
{
    private ModeManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _manager = new ModeManager();
    }

    [Test]
    public void AutoWrapMode_DefaultsToTrue()
    {
        Assert.That(_manager.AutoWrapMode, Is.True);
    }

    [Test]
    public void AutoWrapMode_CanBeSetAndGet()
    {
        _manager.AutoWrapMode = false;
        Assert.That(_manager.AutoWrapMode, Is.False);

        _manager.AutoWrapMode = true;
        Assert.That(_manager.AutoWrapMode, Is.True);
    }

    [Test]
    public void ApplicationCursorKeys_DefaultsToFalse()
    {
        Assert.That(_manager.ApplicationCursorKeys, Is.False);
    }

    [Test]
    public void ApplicationCursorKeys_CanBeSetAndGet()
    {
        _manager.ApplicationCursorKeys = true;
        Assert.That(_manager.ApplicationCursorKeys, Is.True);

        _manager.ApplicationCursorKeys = false;
        Assert.That(_manager.ApplicationCursorKeys, Is.False);
    }

    [Test]
    public void BracketedPasteMode_DefaultsToFalse()
    {
        Assert.That(_manager.BracketedPasteMode, Is.False);
    }

    [Test]
    public void BracketedPasteMode_CanBeSetAndGet()
    {
        _manager.BracketedPasteMode = true;
        Assert.That(_manager.BracketedPasteMode, Is.True);

        _manager.BracketedPasteMode = false;
        Assert.That(_manager.BracketedPasteMode, Is.False);
    }

    [Test]
    public void CursorVisible_DefaultsToTrue()
    {
        Assert.That(_manager.CursorVisible, Is.True);
    }

    [Test]
    public void CursorVisible_CanBeSetAndGet()
    {
        _manager.CursorVisible = false;
        Assert.That(_manager.CursorVisible, Is.False);

        _manager.CursorVisible = true;
        Assert.That(_manager.CursorVisible, Is.True);
    }

    [Test]
    public void OriginMode_DefaultsToFalse()
    {
        Assert.That(_manager.OriginMode, Is.False);
    }

    [Test]
    public void OriginMode_CanBeSetAndGet()
    {
        _manager.OriginMode = true;
        Assert.That(_manager.OriginMode, Is.True);

        _manager.OriginMode = false;
        Assert.That(_manager.OriginMode, Is.False);
    }

    [Test]
    public void Utf8Mode_DefaultsToTrue()
    {
        Assert.That(_manager.Utf8Mode, Is.True);
    }

    [Test]
    public void Utf8Mode_CanBeSetAndGet()
    {
        _manager.Utf8Mode = false;
        Assert.That(_manager.Utf8Mode, Is.False);

        _manager.Utf8Mode = true;
        Assert.That(_manager.Utf8Mode, Is.True);
    }

    [Test]
    public void SetMode_WithKnownMode_UpdatesProperty()
    {
        _manager.SetMode(4, true);  // Insert/Replace mode
        Assert.That(_manager.GetMode(4), Is.True);

        _manager.SetMode(4, false);
        Assert.That(_manager.GetMode(4), Is.False);
    }

    [Test]
    public void SetMode_WithUnknownMode_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _manager.SetMode(999, true));
        Assert.DoesNotThrow(() => _manager.SetMode(999, false));
    }

    [Test]
    public void SetPrivateMode_WithKnownMode_UpdatesProperty()
    {
        _manager.SetPrivateMode(1, true);  // Application cursor keys
        Assert.That(_manager.ApplicationCursorKeys, Is.True);

        _manager.SetPrivateMode(1, false);
        Assert.That(_manager.ApplicationCursorKeys, Is.False);
    }

    [Test]
    public void SetPrivateMode_AutoWrapMode_UpdatesProperty()
    {
        _manager.SetPrivateMode(7, false);  // Auto-wrap mode
        Assert.That(_manager.AutoWrapMode, Is.False);

        _manager.SetPrivateMode(7, true);
        Assert.That(_manager.AutoWrapMode, Is.True);
    }

    [Test]
    public void SetPrivateMode_CursorVisible_UpdatesProperty()
    {
        _manager.SetPrivateMode(25, false);  // Cursor visibility
        Assert.That(_manager.CursorVisible, Is.False);

        _manager.SetPrivateMode(25, true);
        Assert.That(_manager.CursorVisible, Is.True);
    }

    [Test]
    public void SetPrivateMode_WithUnknownMode_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _manager.SetPrivateMode(999, true));
        Assert.DoesNotThrow(() => _manager.SetPrivateMode(999, false));
    }

    [Test]
    public void GetMode_WithSetMode_ReturnsCorrectValue()
    {
        _manager.SetMode(20, true);
        Assert.That(_manager.GetMode(20), Is.True);

        _manager.SetMode(20, false);
        Assert.That(_manager.GetMode(20), Is.False);
    }

    [Test]
    public void GetPrivateMode_WithSetMode_ReturnsCorrectValue()
    {
        _manager.SetPrivateMode(2004, true);  // Bracketed paste mode
        Assert.That(_manager.GetPrivateMode(2004), Is.True);

        _manager.SetPrivateMode(2004, false);
        Assert.That(_manager.GetPrivateMode(2004), Is.False);
    }

    [Test]
    public void SaveAndRestoreModes_PreservesState()
    {
        _manager.SetMode(4, true);
        _manager.SetMode(20, false);

        _manager.SaveModes();

        _manager.SetMode(4, false);
        _manager.SetMode(20, true);

        _manager.RestoreModes();

        Assert.That(_manager.GetMode(4), Is.True);
        Assert.That(_manager.GetMode(20), Is.False);
    }

    [Test]
    public void SaveAndRestorePrivateModes_PreservesState()
    {
        _manager.SetPrivateMode(1, true);
        _manager.SetPrivateMode(25, false);

        _manager.SavePrivateModes();

        _manager.SetPrivateMode(1, false);
        _manager.SetPrivateMode(25, true);

        _manager.RestorePrivateModes();

        Assert.That(_manager.ApplicationCursorKeys, Is.True);
        Assert.That(_manager.CursorVisible, Is.False);
    }

    [Test]
    public void Reset_RestoresDefaultValues()
    {
        // Change all modes from defaults
        _manager.AutoWrapMode = false;
        _manager.ApplicationCursorKeys = true;
        _manager.BracketedPasteMode = true;
        _manager.CursorVisible = false;
        _manager.OriginMode = true;
        _manager.Utf8Mode = false;

        _manager.Reset();

        // Verify all modes are back to defaults
        Assert.That(_manager.AutoWrapMode, Is.True);
        Assert.That(_manager.ApplicationCursorKeys, Is.False);
        Assert.That(_manager.BracketedPasteMode, Is.False);
        Assert.That(_manager.CursorVisible, Is.True);
        Assert.That(_manager.OriginMode, Is.False);
        Assert.That(_manager.Utf8Mode, Is.True);
    }

    [Test]
    public void MultipleSetPrivateMode_CallsWork()
    {
        _manager.SetPrivateMode(7, false);  // Auto-wrap off
        _manager.SetPrivateMode(1, true);  // Application cursor keys on
        _manager.SetPrivateMode(25, false);  // Cursor visible off

        Assert.That(_manager.AutoWrapMode, Is.False);
        Assert.That(_manager.ApplicationCursorKeys, Is.True);
        Assert.That(_manager.CursorVisible, Is.False);
    }
}