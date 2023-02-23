# QT Props Manager
A tool to quickly manage simple prop toggles for VRChat Avatar 3.0.

## Features
- Quickly add simple props to your VRC Avatar 3.0
- Automatically generates only one layer in the FX animator using blend trees
- Automatically generates VRC menu(s) - it adds a paging system if there are too many props
- Automatically generates VRC parameters (1 bool per prop)
- Uses no constraints of any kinds
- Quickly remove all the props that were added with this asset and cleans up animator, VRC menus, and VRC parameters

## Installation
1. Download and add [Animator-As-Code](https://github.com/hai-vr/av3-animator-as-code#install) to your Unity project first.
2. Download and install the latest version of the unity package for QTPropsManager from the [Releases](https://github.com/dbqt/QTPropsManager/releases).
3. That's it for installing!

## Usage

- IMPORTANT: It's recommended that you make a backup of your avatar in case anything goes wrong, this asset modifies directly your avatar!
- Before anything else you need to add the prefab from QTAssets/QTProps/QTPropsManager.prefab to your scene
- Add your avatar's VRCAvatarDescriptor to the manager.
- Customize the QT Props Manager options as needed, the table below describes each property.

### Properties on QT Props Manager

| Property | Required? | Description |
| --- | --- | --- |
| Avatar | Yes | The VRC Avatar Descriptor of the avatar you want to add props to |
| VRC Submenu parent | No | The menu to which QTProps submenu will be added to, if not specified, it will try to add to the default menu |

- For each prop you want to add, click on the + button.
- Fill in the properties for that prop following the table below.
- If a prop is red, that means something is missing or invalid.
- If a prop is yellow, that means the name is not unique.
- If you want to remove a prop, select the prop first, then click on the - button.
- Click Install / Update to install or update all the props on your avatar.
- Click Uninstall to remove all props added using QTPropsManager from your avatar.

### Properties for a prop

| Property | Required? | Description |
| --- | --- | --- |
| Unique name | Yes | Unique name for the prop toggle |
| Prop GameObject | Yes | The GameObject of the prop to be added, a copy will be created on the avatar |
| Bone to attach to | Yes | What bone the prop should be attached to |
| Menu toggle icon | No | Optional texture to be used in the menu for this toggle |
| Default toggle state | - | Whether the prop should be active by default or not |
| Save toggle state | - | Whether the prop's toggle state should persist between avatar loads |

## Dependencies
This asset requires that you install [Animator-As-Code](https://github.com/hai-vr/av3-animator-as-code#install)

## How does it actually work behind the scenes?
This asset helps with the setup of props by automatically attaching the game objects to the bones specified, generating all the blendtrees for the toggle animations, generating VRC menus and VRC parameters, and managing the clean up to revert all of the installation if that's something you want.

## Special thanks
Thank you to [Hai](https://github.com/hai-vr) for creating [Animator-As-Code](https://github.com/hai-vr/av3-animator-as-code#install)
Thank you to the contributors of [this article for writing about using blend trees for toggles](https://notes.sleightly.dev/dbt-combining/)

## License
This is MIT license.