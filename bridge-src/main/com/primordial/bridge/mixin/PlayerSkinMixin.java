/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  net.minecraft.client.player.AbstractClientPlayer
 *  net.minecraft.client.resources.PlayerSkin
 *  org.spongepowered.asm.mixin.Mixin
 *  org.spongepowered.asm.mixin.injection.At
 *  org.spongepowered.asm.mixin.injection.Inject
 *  org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable
 */
package com.primordial.bridge.mixin;

import com.primordial.bridge.ClientSkins;
import net.minecraft.client.player.AbstractClientPlayer;
import net.minecraft.client.resources.PlayerSkin;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(value={AbstractClientPlayer.class})
public abstract class PlayerSkinMixin {
    @Inject(method={"getSkin"}, at={@At(value="HEAD")}, cancellable=true)
    private void primordialSkin(CallbackInfoReturnable<PlayerSkin> callbackInfoReturnable) {
        PlayerSkin playerSkin = ClientSkins.get(((AbstractClientPlayer)(Object)this).getUUID());
        if (playerSkin != null) {
            callbackInfoReturnable.setReturnValue(playerSkin);
        }
    }
}
