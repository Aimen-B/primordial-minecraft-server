from pathlib import Path
import shutil
root=Path(__file__).resolve().parents[1]
page=root/'web/index.html'
text=page.read_text(encoding='utf-8-sig')
start=text.index('      <!-- Instant Whitelist Card -->')
end=text.index('    </div>\n  </section>',start)
text=text[:start]+'''      <div class="whitelist-section"><div class="whitelist-card">
        <h3>Play with approved friends</h3>
        <p>Ask the host to whitelist your exact nickname. Each approved nickname belongs to one player.</p>
      </div></div>
'''+text[end:]
text=text.replace('Type your desired username in the <strong>Whitelist card above</strong> with the invite passcode to get instant access.','Send your exact nickname to the host for approval before joining.')
start=text.index('    // Auto-fill invite code')
end=text.index('</script>',start)
text=text[:start]+text[end:]
page.write_text(text,encoding='utf-8')
for name in ['build-autologin-mod.py','package-autologin.py']:
 path=root/'scripts'/name
 backup=root/'verification'/('obsolete-'+name)
 if path.exists():shutil.copy2(path,backup)
 path.write_text('raise SystemExit("Password chat automation is disabled. Use scripts/build-integration.ps1 for the secure authentication bridge.")\n')
path=root/'scripts/build-candidate-client.ps1'
text=path.read_text()
text=text.replace("-Destination $distDir\nSet-Content", "-Destination (Join-Path $distDir 'PrimordialLauncher.exe')\nSet-Content")
text=text.replace("-Destination $releaseDir\n$assets", "-Destination (Join-Path $releaseDir 'PrimordialLauncher.exe')\n$assets")
path.write_text(text)
print('Removed misleading self-service website UI and disabled obsolete password-chat builds.')
