# Bare-bones Pi render test: low-poly ships, a low-poly planet, a starfield, one sun, StandardMaterial3D only.
extends Node3D
var ships := []
var t := 0.0
var frames := 0
var acc := 0.0
var fps_list := []
func _ready():
	var n := 40
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--ships="): n = int(a.substr(8))
	var env := WorldEnvironment.new(); var e := Environment.new()
	e.background_mode = Environment.BG_COLOR; e.background_color = Color(0.02,0.02,0.05)
	e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR; e.ambient_light_color = Color(0.25,0.25,0.35)
	e.fog_enabled = "--fog" in OS.get_cmdline_user_args(); e.fog_light_color = Color(0.3,0.2,0.5); e.fog_density = 0.002
	env.environment = e; add_child(env)
	var sun := DirectionalLight3D.new(); sun.rotation_degrees = Vector3(-40, 30, 0); add_child(sun)
	var cam := Camera3D.new(); cam.position = Vector3(0, 30, 90); cam.look_at_from_position(cam.position, Vector3.ZERO); add_child(cam)
	var planet := MeshInstance3D.new(); var sm := SphereMesh.new(); sm.radius = 60; sm.height = 120; sm.radial_segments = 24; sm.rings = 12
	planet.mesh = sm; var pm := StandardMaterial3D.new(); pm.albedo_color = Color(0.3,0.5,0.8); planet.material_override = pm
	planet.position = Vector3(-120, -40, -250); add_child(planet)
	# starfield: one MultiMesh of small quads
	var mm := MultiMesh.new(); mm.transform_format = MultiMesh.TRANSFORM_3D; var q := QuadMesh.new(); q.size = Vector2(0.8,0.8)
	var qm := StandardMaterial3D.new(); qm.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED; qm.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	q.material = qm; mm.mesh = q; mm.instance_count = 3000
	var rng := RandomNumberGenerator.new(); rng.seed = 7
	for i in 3000:
		mm.set_instance_transform(i, Transform3D(Basis(), Vector3(rng.randf_range(-1,1), rng.randf_range(-1,1), rng.randf_range(-1,1)).normalized() * 600))
	var mmi := MultiMeshInstance3D.new(); mmi.multimesh = mm; add_child(mmi)
	# ships: low-poly prisms (about 200 triangles each)
	for i in n:
		var s := MeshInstance3D.new(); var c := CylinderMesh.new(); c.top_radius = 0.2; c.bottom_radius = 2.0; c.height = 8; c.radial_segments = 6
		s.mesh = c; var m := StandardMaterial3D.new(); m.albedo_color = Color.from_hsv(rng.randf(), 0.4, 0.9); m.metallic = 0.5; m.roughness = 0.5
		s.material_override = m; s.rotation_degrees.x = -90; add_child(s); ships.append([s, rng.randf()*TAU, rng.randf_range(15,50), rng.randf_range(-10,10)])
func _process(dt):
	t += dt
	for e in ships:
		var a: float = e[1] + t * 20.0 / e[2]
		e[0].position = Vector3(cos(a)*e[2], e[3], sin(a)*e[2]); e[0].rotation.y = -a
	frames += 1; acc += dt
	if acc >= 1.0:
		fps_list.append(frames); print("FPS ", frames); frames = 0; acc = 0.0
	if t > 25.0:
		fps_list.sort(); print("RESULT min=%d median=%d max=%d" % [fps_list[1], fps_list[fps_list.size()/2], fps_list[-1]]); get_tree().quit()
