from pathlib import Path
root=Path(__file__).resolve().parents[1]
path=root/'bridge-src/auth/com/codex/forgelogin/AuthHooks.java'
text=path.read_text()
point='        boolean bl4 = bl2 ='
offset=text.index(point)
text=text[:offset]+'''        Field usersField=object.getClass().getDeclaredField("users");usersField.setAccessible(true);
        Properties users=(Properties)usersField.get(object);String previous=users.getProperty(string3);
'''+text[offset:]
text=text.replace('        return bl2 ? "ok"', '        if(bl && !bl2) { if(previous==null)users.remove(string3);else users.setProperty(string3,previous); }\n        return bl2 ? "ok"')
start=text.index('    public static boolean atomicSave(')
text=text[:start]+'''    public static boolean atomicSave(Properties users,Path path) {
        Path temp=null;
        try {
            Files.createDirectories(path.getParent());
            temp=Files.createTempFile(path.getParent(),"users-",".tmp");
            try(OutputStream output=Files.newOutputStream(temp)) {users.store(output,"ForgeLoginMod users. Password hashes.");}
            try(FileChannel channel=FileChannel.open(temp,StandardOpenOption.WRITE)){channel.force(true);}
            Files.move(temp,path,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);return true;
        }catch(IOException error){return false;}
        finally {if(temp!=null)try{Files.deleteIfExists(temp);}catch(IOException ignored){}}
    }
}
'''
path.write_text(text)
print('Authentication persistence recovered with failed-write rollback.')
