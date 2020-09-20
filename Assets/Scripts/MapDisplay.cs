using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class MapDisplay : MonoBehaviour {

	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRender;

    public void DrawTexture(Texture2D texture) {
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
	}

	public void DrawMesh(MeshData data, Texture2D texture) {
		meshFilter.sharedMesh = data.CreateMesh();
		meshRender.sharedMaterial.mainTexture = texture;
    }
}
