# Fleet Command

A real-time tactical starship combat game in Godot. You captain one ship from a console of panels
(the helm, the ship systems display, weapons and power) beside a live 3D view of the battle. It
plays on desktop and phones: any panel can be expanded to full screen, with the battle view moved
to the side.

Status: ground rules, tools and skills are set up; the first three.js prototypes come next.

- Rules for contributors (and Claude): [CLAUDE.md](CLAUDE.md)
- Open questions: [docs/design/README.md](docs/design/README.md)
- Specs and changes: [openspec/](openspec/)

## Tools

```bash
scripts/install-godot.sh        # Godot 4.7.1 (build.config) into .tools/godot/
scripts/install-blender.sh      # Blender 4.4.3 into /opt/blender
npm install -g @fission-ai/openspec@1.14.0
```

Mockups: copy `docs/mockups/lib/template.html`, then screenshot with
`NODE_PATH=$(npm root -g) node tools/mockups/shoot.mjs docs/mockups/<page>.html docs/screenshots/mockups`.
