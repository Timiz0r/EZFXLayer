//eventually comes per-avatar settings. first, we won't allow overriding, just adding new layers. scene comes
//first, then avatar.
//for asset generation, we previously shucked everything in a folder next to the fx controller. ofc, not everyone
//necessarily follows my personal convention. instead, we'll shuck everything in a folder next to the scene
//  Scene.unity
//  EZFXLayer
//    generation (temp for current generation)
//    working
//      AvatarName
//an initial thought was to have Scene folder and per-avatar folders. then it was to not to keep it simple.
//but it might be simpler to have these two kinds of folders, since it's already coded for avatars to share.
//perhaps the biggest struggle is getting per-avatar fx controllers, since generator doesnt know how to duplicate
//i mean we coded something for unit tests, but it wouldn't be surprising for it to not work fully.
//one option is to IAssetRepository.Duplicate certain assets.
//
//now, the scenario is to be able to add things only to certain avatars. later, if we choose to expand and
//design around it, perhaps also to turn things off for certain avatars. i think the main reason would be
//menu cleanliness. functionally, if a menu doesn't work, then it doesn't harm. if the menu isn't used, it's
//just clutter. also maybe an avatar has a specific, non-default value that must always be used.
//
//first, we'll do a scene specific generation, passing all avatars and ignoring avatar-specific layers.
//then, for each avatar, we'll get their specific layers. if any, we'll create a new generator and only generate for
//that specific avatar. for the 3 magic artifacts, we'll duplicate the scene-based generation. here, the scene-based clips
//remain the same, which is nice.
//
//needs some design thought on how to override, more or less, what's in the scene-based stuff.

//plan a ver 2, if we think perf wouldn't be a concern, is to only generate per-avatar, which  would be the simplest
//by-far to implement.
//
//also gives us flexibility in avatar-specific components to change what's in the scene-level components, such as
//removing layers from the generation entirely.

//plan b in case we hit perf issues
//with old ezfxlayer, it takes a few seconds to generate (3-5?). this is somewhat slow, and there werent a lot of layers
//didn't investigate, but i feel like it was the copying of everything over that took the most time -- assetdb creation.
//it could be that maintaining existing animation assets improves perf. in any case, we'll know once we have a working v2.
//this method theoretically has better perf because we have scene-level assets -- animations and fx layer.
//tho may have just thought of a way to improve perf for plan a!
//
//for generator configuration, we'll probably produce a list in top-down order like before. each layer will have a scope
//that it applies to -- scene and avatar-specific. for each avatar, we'll select the in-scope layers. we'll choose to
//sort based on scope, as well. i don't think there's a scenario where we want avatar layers first, so we'll make sure
//it can't be the case. this is presumably more straightforward to think about and should cause less confusion.
//
//we'll use the same fx layer for all avatars, to keep things simple on our end.
//to customize things per-avatar, we'll generate avatar-specific menus and parameters. adding stuff works as expected.
//we'll also add a component to force a layer to a specific animation. in effect, this removed the toggles for that layer,
//and, if the menu is empty, we'll remove the menu (if generated and not premade?). expression parameters also get that
//value. we'll also have a component ui utility to select avatars to apply this new component to.
//
//the downside of this all ofc is complexity of implementation. we'd only do it if perf proved to be a concern.
