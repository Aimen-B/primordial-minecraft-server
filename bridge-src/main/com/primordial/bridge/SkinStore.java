/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  com.codex.forgelogin.AuthHooks
 *  net.neoforged.fml.loading.FMLPaths
 */
package com.primordial.bridge;

import com.codex.forgelogin.AuthHooks;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.OpenOption;
import java.nio.file.Path;
import java.util.Properties;
import java.util.Set;
import java.util.UUID;
import net.neoforged.fml.loading.FMLPaths;

public final class SkinStore {
    public static final Set<String> IDS = Set.of("default","gentleman_duck","cozy_frog","toasty_bread","neko_kitty","derpy_banana","baby_dino","gentleman_penguin","strawberry_cow","red_panda","space_axolotl","ember_mage","frost_mage","shadow_rogue","crystal_knight","forest_ranger","copper_robot");
    private final Properties skins = new Properties();
    private final Path path = FMLPaths.CONFIGDIR.get().resolve("primordial_bridge/skins.properties");

    public SkinStore() {
        if (Files.exists(this.path, new LinkOption[0])) {
            try (InputStream inputStream = Files.newInputStream(this.path, new OpenOption[0]);){
                this.skins.load(inputStream);
            }
            catch (IOException iOException) {
                throw new IllegalStateException("Could not load player skin choices", iOException);
            }
        }
    }

    public String get(UUID uUID) {
        return this.skins.getProperty(uUID.toString(), "default");
    }

    public void set(UUID uUID, String string) {
        if (!IDS.contains(string)) {
            throw new IllegalArgumentException("Unknown skin");
        }
        String string2 = this.skins.getProperty(uUID.toString());
        this.skins.setProperty(uUID.toString(), string);
        if (!AuthHooks.atomicSave((Properties)this.skins, (Path)this.path)) {
            if (string2 == null) {
                this.skins.remove(uUID.toString());
            } else {
                this.skins.setProperty(uUID.toString(), string2);
            }
            throw new IllegalStateException("Could not save skin selection");
        }
    }
}

