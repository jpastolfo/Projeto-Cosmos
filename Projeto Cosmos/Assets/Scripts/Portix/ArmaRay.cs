using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ArmaRay : MonoBehaviour
{
    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-//
    //PROJETIL, BALA, BULLET
    [Header("Prefabs de Muni��es")]
    [SerializeField] public GameObject Bullet;
    [SerializeField] public GameObject Projectile;
    [SerializeField] private GameObject CurrentProjectile;
    [SerializeField] public GameObject CanvasOverHeat;
    [SerializeField] public GameObject CanvasMunicaoMissil;    
    public GameObject inimigo;

    //FORCA DA BALA, BULLET FORCE
    [Header("Configura��es Arma")]
    public float shootForce = 200f;

    //CONSTITUICAO DA ARMA, GUN STATS
    public float shootingRate; //tempo entre as disparadas
    public float fireRate; //tempo entre os tiros
    public float spread; //dispersao do tiro
    public float reloadTime; //tempo de recarregat
    public int magazineSize; //tamanho do pente
    public int bulletPerTap; //quantas balas saem por clique
    public bool allowHold; //auto / semiauto
    public bool hasOverHeat = true;
    public bool hasAmmo = false;
    int bulletsLeft, bulletsShot; //quantas balas tem
    public int extraAmmo;
    private float overHeatReload = 0.25f;
    public float overHeat= 0f;
    private int maxHeat = 25;

    //BOOLS CHECKS
    bool shooting, reloading, readyToShoot;
    public bool isOverHeating;
    public bool readyToLock; 

    //REFERENCES
    [Header("Refer�ncias")]
    public Camera fpsCam;
    public Transform primaryWeaponPoint;
    public Transform secondaryWeaponPoint;
    public Transform firePoint;

    //GRAFICO
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammoDisplay;
    public LayerMask PlayerLayerMask;

    //BUG FIXING
    public bool allowInvoke = true;
    public Vector3 mousePos;
    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-//

    //Referencias/Scripts
    [Header("Scripts")]
    public OverHeatBar overHeatBar;
    public TargetController targetControllerScript;


    private void Awake()
    {
        //TER CERTEZA SE O PENTE TA FULL
        readyToLock = true;
        bulletsLeft = magazineSize;
        readyToShoot = true;
        isOverHeating = false;
        CurrentProjectile = Bullet;
        firePoint = primaryWeaponPoint;
        extraAmmo = 2 * magazineSize;
        StartCoroutine(OverHeat());
    }
    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-//

    private void Update()
    {
        MyInput();
        if(hasOverHeat)
        {
            overHeatBar.SetOverheat(overHeat);
        }
        //set ammo display
        if (ammoDisplay != null)
            ammoDisplay.SetText(bulletsLeft + "/" + extraAmmo);

        if (!targetControllerScript.lockedOn)
            CheckHit();
        
    }
    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-//
    private void MyInput()
    {
        //CHECAR SE PODE SENTA A PUA
        if (allowHold)
            shooting = Input.GetKey(KeyCode.Mouse0);
        else
            shooting = Input.GetKeyDown(KeyCode.Mouse0);
        //*
        //RECARREGAR MANUAL
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading && !hasOverHeat)
            Reload();
        //RECARGA AUTOMATICA
        if (readyToShoot && !reloading && bulletsLeft <= 0 && !hasOverHeat)
            Reload();
        //*/
        //ATIRANDO, SHOOTING
        if (!isOverHeating && readyToShoot && shooting && !reloading && overHeat < maxHeat && hasOverHeat)
        {
            //NAO ATIROU NENHUMA, ainda
            bulletsShot = 0;
            Shoot();
        }
        //*
        else if (readyToShoot && shooting && !reloading && hasAmmo)
        {
            //NAO ATIROU NENHUMA, ainda
            bulletsShot = 0;
            Shoot();
        }
        //*/
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ShootPrimaryWeapon();
            hasOverHeat = true;
            hasAmmo = false;
            //*
            if (overHeatReload == 0.125f)
            {
                isOverHeating = true;
            }
            //*/
            CanvasOverHeat.SetActive(true);
            CanvasMunicaoMissil.SetActive(false);
            
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ShootMissile();
            hasOverHeat = false;
            hasAmmo = true;
            isOverHeating = false;
            CanvasOverHeat.SetActive(false);
            CanvasMunicaoMissil.SetActive(true);
        }


    }
    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-//

    private void Shoot()
    {
        //ja atirou
        readyToShoot = false;

        //ENCONTRAR A POSICAO ACERTADA DO CURSOR USANDO RAYCAST
        //origem do ray eh o meio do player
        //aponta para a posicao do cursor
        float rayLength = 10000f;//distancia infinita onde o z aponta
        Ray ray =  Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //CHECAR SE O RAY MIRA EM ALGO
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit, rayLength, PlayerLayerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100);//just away from the player
        
        //CALCULO DA DIRECAO DA NAVE ATE O ALVO
        Vector3 directionNOSpread = targetPoint - firePoint.position;

        //CALCULO DO SPREAD
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        //direcao com spread
        Vector3 directionSpread = directionNOSpread + new Vector3(x,y,0);

        //INSTANCIAR A BALA, INSTANCIATE BULLET
        GameObject currentBullet = Instantiate(CurrentProjectile, firePoint.position, Quaternion.identity);
        //rodar a bala na direcao correta
        currentBullet.transform.forward = directionSpread.normalized;

        //ADD FORCES TO BULLET
        currentBullet.GetComponent<Rigidbody>().AddForce(directionSpread.normalized * shootForce, ForceMode.Impulse);

        //INSTANCIAR muzzleFlash
        if (muzzleFlash != null)
            Instantiate(muzzleFlash, transform);// firePoint.position), Quaternion.identity);

        //DESCONTAR DAS BALAS E MARCAR Q ATIROU
        bulletsShot++;
        if (hasOverHeat)
            overHeat++;
        //*
        else if (hasAmmo)
        {
            bulletsLeft--;
            bulletsShot++;
        }
        //*/

        //RESET DO SHOOT
        if (allowInvoke)
        {
            Invoke("ResetShot", shootingRate);
            allowInvoke = false;

        }

        //SE TEM MAIS DE UMA BALA POR CLICK
        if (bulletsShot < bulletPerTap && bulletsLeft > 0)
            Invoke("Shoot", shootingRate);
    }

    private void ResetShot()
    {
        //allow shooting and invoking again
        readyToShoot = true;
        allowInvoke = true;
    }
    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-//
    //*
    private void Reload()
    {
        reloading = true;
        Invoke("ReloadFinished", reloadTime);
    }
    //*/
    //OverHeat na arma, em vez de muni��o  -> arma de overheat acertando a nave principal
    // Laser
    // Missil Teleguiado

    //*
    private void ReloadFinished()
    {
        if ((magazineSize - bulletsLeft) > extraAmmo) // Arrumar, ainda est� dando n�mero negativo ??
        {
            bulletsLeft += extraAmmo;
            extraAmmo = 0;
        }
        else if (bulletsLeft != 0)
             {
                 extraAmmo -= (magazineSize - bulletsLeft);
                 bulletsLeft += (magazineSize - bulletsLeft);
             }
             else
             {
                 extraAmmo -= magazineSize;
                 bulletsLeft += magazineSize;
             }  

        reloading = false;
    }
    //*/

    private void ShootPrimaryWeapon()
    {
        CurrentProjectile = Bullet;
        firePoint = primaryWeaponPoint;
        shootForce = 200f;
        shootingRate = 0.1f;
    }

    private void ShootMissile()
    {
        CurrentProjectile = Projectile;
        firePoint = secondaryWeaponPoint;
        shootForce = 0;
        shootingRate = 0.5f;
    }

    private void CheckHit()
    {
        float rayLength = 400f;//distancia infinita onde o z aponta
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //CHECAR SE O RAY MIRA EM ALGO
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit, rayLength, PlayerLayerMask))
        {
            targetPoint = hit.point;
            //Debug.Log("Acertou");
            Debug.Log(hit.collider.gameObject.name);
            inimigo = hit.collider.gameObject;

        }
        else
            targetPoint = ray.GetPoint(100);//just away from the player
    }

    IEnumerator OverHeat()
    {
        yield return new WaitForSeconds(overHeatReload);
        if(!shooting || reloading || isOverHeating)
        {
            if (overHeat > 0)
            {
                overHeat -= 0.5f;
                readyToShoot = true;
            }
        }
        if (overHeat >= maxHeat)
        {
            overHeatReload = 0.125f;
            readyToShoot = false;
            isOverHeating = true;
            StartCoroutine(OverHeated());
        }
        
        StartCoroutine(OverHeat());
    }

    IEnumerator OverHeated()
    {
        yield return new WaitForSeconds(5f);
        isOverHeating = false;
        readyToShoot = true;
        overHeatReload = 0.5f;
    }
}
