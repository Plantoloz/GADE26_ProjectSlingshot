 using UnityEngine;                                                                                                                                                                 
                                                                                                                                                                                     
  public class PlanetSpawner : MonoBehaviour                                                                                                                                         
  {                                                                                                                                                                                  
      public GravityBody sun;
                                                                                                                                                                                     
      void Start()
      {                                                                                                                                                                              
          GravityBody[] planets = FindObjectsByType<GravityBody>(FindObjectsSortMode.None);                                                                                          
          foreach (var body in planets)                                                                                                                                              
          {                                                                                                                                                                          
              if (body == sun) continue;                                                                                                                                             
                                                                                                                                                                                     
              Vector3 toBody = body.rb.position - sun.rb.position;                                                                                                                   
              float r = toBody.magnitude;                                                                                                                                            
                                                                                                                                                                                     
              // Kreisbahngeschwindigkeit
              float speed = Mathf.Sqrt(GravityBody.G * sun.rb.mass / r);                                                                                                             
                                                                                                                                                                                     
              // Senkrecht zur Verbindungslinie (im 2D-XY-Plane)                                                                                                                     
              Vector3 tangent = new Vector3(-toBody.y, toBody.x, 0f).normalized;                                                                                                     
              body.rb.linearVelocity = tangent * speed;                                                                                                                              
          }       
      }                                                                                                                                                                              
  }   