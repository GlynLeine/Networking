This project contains the base code for a graphical avatar lobby system.
For the assignment you'll have to implement a server, the ChatLobbyClient class 
and use/extend the shared library to implement a network protocol layer to
implement the requested features.

All the required view code is provided in the viewmanagement package,
and the AvatarAreaManagerTester and the given ChatLobbyClient demonstrate how to you that layer.

In short, you can get a reference to the important components in your scene using:
        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _panelWrapper = FindObjectOfType<PanelWrapper>();

Register for important events like this:
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;
        _panelWrapper.OnChatTextEntered += onChatTextEntered;

And call important functionality like this:
        _avatarAreaManager.AddAvatarView(int pId) : AvatarView
        _avatarAreaManager.GetAvatarView (int pAvatarId) : AvatarView
        _avatarAreaManager.RemoveAvatarView (int pAvatarId) : void
        _avatarAreaManager.GetAllAvatarIds () : List<int> 

        _avatarView.Move(Vector3 pEndPosition)
        _avatarView.SetSkin (int pSkin)
        _avatarView.Say (string pText)

You can inspect the details of the classes for more information, but it is not required to finish the course.