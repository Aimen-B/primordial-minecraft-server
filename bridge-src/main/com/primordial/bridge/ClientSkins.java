/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  net.minecraft.client.resources.PlayerSkin
 *  net.minecraft.client.resources.PlayerSkin$Model
 *  net.minecraft.resources.ResourceLocation
 */
package com.primordial.bridge;

import com.primordial.bridge.SkinStore;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;
import net.minecraft.client.resources.PlayerSkin;
import net.minecraft.resources.ResourceLocation;

public final class ClientSkins {
    private static final Map<UUID, String> choices = new HashMap<UUID, String>();

    public static void clear() {
        choices.clear();
    }

    public static void set(String string, String string2) {
        if (SkinStore.IDS.contains(string2)) {
            choices.put(UUID.fromString(string), string2);
        }
    }

    public static PlayerSkin get(UUID uUID) {
        String string = choices.getOrDefault(uUID, "default");
        if (string.equals("default")) {
            return null;
        }
        return new PlayerSkin(ResourceLocation.fromNamespaceAndPath((String)"primordial_bridge", (String)("textures/skins/" + string + ".png")), null, null, null, PlayerSkin.Model.WIDE, false);
    }
}

