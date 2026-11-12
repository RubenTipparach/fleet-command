# Cube-marched nebula for the Pi test: a 3D noise density field, polygonised once at load with naive surface nets
# (one vertex per cell the surface crosses, at the mean of its edge crossings; one quad per crossed lattice edge) into
# nested shells at rising density levels, each drawn unshaded and alpha-blended with a soft rim fade. No per-pixel noise.
extends Node3D
const N := 40            # grid cells per side
const SIZE := 260.0      # cloud extent in world units
var levels := [0.10, 0.25, 0.40]
var colors := [Color(0.45, 0.25, 0.75, 0.10), Color(0.75, 0.35, 0.65, 0.14), Color(1.0, 0.65, 0.55, 0.20)]
var tris := 0

func _ready():
	var noise := FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH; noise.frequency = 0.035; noise.fractal_octaves = 4; noise.seed = 11
	var f := PackedFloat32Array(); f.resize((N + 1) * (N + 1) * (N + 1))
	var c := N / 2.0
	for z in N + 1:
		for y in N + 1:
			for x in N + 1:
				var p := Vector3(x - c, (y - c) * 1.6, z - c) / c   # flattened ellipsoid falloff
				var fall: float = 1.0 - p.length()
				f[_i(x, y, z)] = noise.get_noise_3d(x, y, z) * 0.8 + fall * 0.6 - 0.1
	for k in levels.size():
		add_child(_shell(f, levels[k], colors[k]))
	print("NEBULA shells=%d triangles=%d" % [levels.size(), tris])

func _i(x, y, z) -> int: return x + (N + 1) * (y + (N + 1) * z)

func _shell(f: PackedFloat32Array, iso: float, col: Color) -> MeshInstance3D:
	var cell_v := {}   # cell index -> vertex index
	var verts := PackedVector3Array()
	var norms := PackedVector3Array()
	var corners := [Vector3i(0,0,0),Vector3i(1,0,0),Vector3i(0,1,0),Vector3i(1,1,0),Vector3i(0,0,1),Vector3i(1,0,1),Vector3i(0,1,1),Vector3i(1,1,1)]
	var edges := [[0,1],[2,3],[4,5],[6,7],[0,2],[1,3],[4,6],[5,7],[0,4],[1,5],[2,6],[3,7]]
	var scale := SIZE / N
	for z in N:
		for y in N:
			for x in N:
				var v := []
				var inside := 0
				for k in 8:
					var cc: Vector3i = corners[k]
					var d: float = f[_i(x + cc.x, y + cc.y, z + cc.z)] - iso
					v.append(d)
					if d > 0: inside += 1
				if inside == 0 or inside == 8: continue
				var sum := Vector3.ZERO; var n := 0
				for e in edges:
					var a: float = v[e[0]]; var b: float = v[e[1]]
					if (a > 0) != (b > 0):
						var t := a / (a - b)
						sum += Vector3(corners[e[0]]).lerp(Vector3(corners[e[1]]), t); n += 1
				var p := (Vector3(x, y, z) + sum / n - Vector3.ONE * N / 2.0) * scale
				var g := Vector3(v[1]+v[3]+v[5]+v[7]-v[0]-v[2]-v[4]-v[6], v[2]+v[3]+v[6]+v[7]-v[0]-v[1]-v[4]-v[5], v[4]+v[5]+v[6]+v[7]-v[0]-v[1]-v[2]-v[3])
				cell_v[x + N * (y + N * z)] = verts.size(); verts.append(p); norms.append(-g.normalized())
	var idx := PackedInt32Array()
	# one quad per lattice edge whose ends straddle the level, joining the 4 cells around it
	for z in range(1, N):
		for y in range(1, N):
			for x in range(1, N):
				var d0: float = f[_i(x, y, z)] - iso
				for axis in 3:
					var o: Vector3i = [Vector3i(1,0,0), Vector3i(0,1,0), Vector3i(0,0,1)][axis]
					var d1: float = f[_i(x + o.x, y + o.y, z + o.z)] - iso
					if (d0 > 0) == (d1 > 0): continue
					var q := []
					match axis:
						0: q = [Vector3i(x,y-1,z-1), Vector3i(x,y,z-1), Vector3i(x,y,z), Vector3i(x,y-1,z)]
						1: q = [Vector3i(x-1,y,z-1), Vector3i(x-1,y,z), Vector3i(x,y,z), Vector3i(x,y,z-1)]
						2: q = [Vector3i(x-1,y-1,z), Vector3i(x,y-1,z), Vector3i(x,y,z), Vector3i(x-1,y,z)]
					var ids := []
					for cc in q:
						var key: int = cc.x + N * (cc.y + N * cc.z)
						if not cell_v.has(key): break
						ids.append(cell_v[key])
					if ids.size() < 4: continue
					idx.append_array([ids[0], ids[1], ids[2], ids[0], ids[2], ids[3]])
	tris += idx.size() / 3
	var arr := []; arr.resize(Mesh.ARRAY_MAX)
	arr[Mesh.ARRAY_VERTEX] = verts; arr[Mesh.ARRAY_NORMAL] = norms; arr[Mesh.ARRAY_INDEX] = idx
	var m := ArrayMesh.new(); m.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arr)
	var sh := Shader.new()
	sh.code = """shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never;
uniform vec4 col : source_color;
void fragment() {
	float rim = abs(dot(normalize(NORMAL), normalize(VIEW)));
	ALBEDO = col.rgb; ALPHA = col.a * rim;
}"""
	var mat := ShaderMaterial.new(); mat.shader = sh; mat.set_shader_parameter("col", col)
	var mi := MeshInstance3D.new(); mi.mesh = m; mi.material_override = mat
	return mi
