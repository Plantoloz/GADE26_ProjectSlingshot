Setup-Schritte

1. Layer anlegen

Project Settings → Tags and Layers → neuer Layer: Minimap

2. Minimap-Kamera

- Neues GameObject: Minimap Camera
- Komponente: MinimapCamera
- Camera-Komponente: Culling Mask = nur Minimap
- cameraZ = -500 (hinter der Szene)
- defaultView = neues ScriptableObject (Assets → Create → Minimap → View Config)

3. UI einrichten

- Canvas → RawImage
- Auf das MinimapCamera-GO → OutputTexture wird im Play Mode befüllt
- Im Start() eines UI-Scripts: rawImage.texture = MinimapCamera.Instance.OutputTexture

4. Pfadlinie

- Neues GameObject: Minimap Path
- Komponenten: LineRenderer + MinimapPathRenderer
- mapPath = dein MapPath-Objekt zuweisen

5. Icons (Schiff, Planeten, Checkpoints)

- MinimapIcon-Komponente auf jedes Objekt
- minimapLayer = "Minimap", Sprite und Farbe setzen

6. Trigger für dynamische Views (z.B. Slingshot-Planet)

GameObject (mit BoxCollider oder SphereCollider)
└── MinimapTrigger
enterFollowTarget: → Planet-Transform (kamera folgt dem Planeten)
exitView:          → leer (kehrt zu defaultView zurück)

