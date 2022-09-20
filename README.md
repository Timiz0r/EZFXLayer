# EZFXLayer
EZFXLayer is a tool used in the authoring of VRChat avatars. It simplifies the authoring of toggles that would involve
customizing VRChat avatars' FX playable layer, distilling animator controller, animation, expression menu,
and expressions parameters creation and modification into simple configurations.



## Quick links
[Manual](https://github.com/Timiz0r/EZFXLayer/wiki/Manual)
[説明書](https://github.com/Timiz0r/EZFXLayer/wiki/%E8%AA%AC%E6%98%8E%E6%9B%B8)

## Overview

EZFXLayer is configured by adding a series of GameObjects to a scene containing EZFXLayer components.
Note that these configurations are outside of avatar-related GameObjects due to limitation of VRCSDK.

### Reference configuration
The first of these configurations is the `reference configuration`. There is *one per scene*, and it mainly points
to the FX playable layer's animator controller, VRC expression menu, and VRC expressions parameter that
will be generated on top of. It is also the location of other generation options and a generate button.

![the reference configuration component](./docs/readme_img/reference_configuration.png)

### Animator layer configuration
The other configuration is the `animator layer configuration` and is where most of the configuration happens.
It mainly contains the name of the layer, the menu path into which toggles will be added,
and a series of `animation configurations`. Also note that these are evaluated in the order in which they
are encountered within the scene.

![the animator layer configuration component](./docs/readme_img/layer_configuration.png)

#### Animation configuration
`Animation configurations` (again, part of the layer configuration) contain a series of `GameObjects` and `blendshapes`,
plus their expected value for that animation. They also allow for customizing the value of the toggle menu control,
by default the name of the animation configuration.
The `reference animation` is special in that it defines the full set of `GameObjects` and `blendshapes`
that are available to other animations. All animations in the layer are kept consistent,
and it is not possible, nor usually desired anyway, to have different sets of these across a set of animations.
No more forgetting to modify all animations!

![the animator layer configuration component](./docs/readme_img/adding_gameobject.png)

### Generation
Generation can be invoked from the `reference configuration` component in the scene. Some key aspects:
* The reference animator controller, menu, and parameters are not modified. Instead, they are copied.
* If the main generation process succeeds, the copies of these artifacts are saved elsewhere.
* All avatars in the scene are modified to use these newly generated artifacts.
* Avatar prefabs are supported.
* Undo/redo is fully supported for UI, generation (theoretically).

#### Minimal modifications
Great care is taken to minimally touch the FX playable layer to allow for user customization
* Animator layers without a matching EZFXLayer `animator layer configuration` are not touched.
* Animator states without a matching EZFXLayer `animation configuration` are removed, since EZFXLayer doesn't know
  how to deal with their transitions and wants to avoid conflicts. Again, the reference animator controller is
  untouched.
  * If there is a match, only the Motion is changed, and the rest of the configuration is maintained.
* Animator transitions, aside from conditions, are not touched.
* Animator layers are added in the order they are configured and relative to what's in the reference animator controller.
  * It is possible to add empty `animator layer configurations` that refer to layers in the reference animator controller,
    in order to force EZFXLayer to generate the next `animator layer configuration's` animator layer in the right place.

Furthermore, all menus and submenus are copied over, and existing submenus are used if they match
what's configured for the `animator layer configuration`. In other words, EZFXLayer will generate toggles alongside
user-customized menus, and EZFXLayer will generate new submenus if they do not yet exist.

## Development
Uses the same Unity version as VRChat: 2019.4.31.

### VS Code
The Unity project uses a forked VSCode extension that is resposible for generating .props files containing Reference
items. This allows for a few things:
* SDK-style projects
* Adding files without having to go to Unity Editor and regenerate projects.
* Full hinting and whatnot.
* `dotnet build` support, though can't run anything, of course. Mostly useless in practice.

There is an .sln in `src/sln` useful for VS Code.

No known way of debugging.

### VS
Usual VS support that can be used by opening the root .sln.
See also: [VS Unity Tools](https://visualstudio.microsoft.com/vs/unity-tools/)

### Tests and design
This project generally uses a ports-and-adapters design, where `EZFXLayer.Editor/Generator` is the main application.
The `EZFXLayerGenerator` is the main driven port. Its main driver port is `IAssetRepository`.
VRC and animation artifacts are considered to be within the scope of the application, so no abstraction is done around
them. However, `IAssetRepository` rids us of an `AssetDatabase` dependency within the application.

Unit tests are a driven adapter of the application, so all tests interact with the application at the
`EZFXLayerGenerator`-level.

Incidentally, `GeneratorRunner`, used by the editor to run `EZFXLayerGenerator`, is the other driven adapter.

The UI isn't unit tested, though it ended up being complex enough that we should probably attempt it.

### Releases
There is no CI, and releases are created manually. The scale of the project requires neither of these yet.

Packages are automatically created in Azure Artifacts. It is done there because anonymous auth is supported, including
for their API. OpenUPM is an option not currently being used.