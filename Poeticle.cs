using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using System;

public class Poeticle : UdonSharpBehaviour
{
    [TextArea(3, 10)]
    [Tooltip("テキストを入力してください。各行は別々のパーティクルとして表示されます")]
    public string inputText = "こんにちは\nVRChat\nテキスト\nパーティクル";
    
    [Header("パーティクル設定")]
    public float spawnRadius = 2.0f;
    public float lifetimeMin = 5.0f;
    public float lifetimeMax = 10.0f;
    public float sizeMin = 0.1f;
    public float sizeMax = 0.3f;
    public float floatSpeed = 0.5f;
    public float rotationSpeed = 15.0f;
    
    [Header("見た目の設定")]
    public Material textMaterial;
    public Color textColor = Color.white;
    public int fontSize = 24;
    
    [Header("パーティクルシステム設定")]
    [Tooltip("パーティクルのプレハブ（ParticleSystemコンポーネント付き）")]
    public GameObject particlePrefab;
    
    [Tooltip("テクスチャアトラス用のテクスチャ - 文字を描画したテクスチャを指定")]
    public Texture2D textureAtlas;
    
    [Tooltip("最大表示数")]
    public int maxParticles = 50;
    
    // 内部で使用する変数
    private ParticleSystem particleSystem;
    private ParticleSystemRenderer particleRenderer;
    private string[] textLines;
    private int textLineCount;
    
    void Start()
    {
        // テキストを分割
        textLines = inputText.Split('\n');
        textLineCount = textLines.Length;
        
        if (particlePrefab == null)
        {
            Debug.LogError("[Poeticle] パーティクルプレハブが設定されていません");
            return;
        }
        
        // パーティクルシステムをセットアップ
        SetupParticleSystem();
        
        // パーティクルを生成
        EmitTextParticles();
    }
    
    public void SetupParticleSystem()
    {
        // プレハブからパーティクルシステムをインスタンス化
        GameObject particleObj = VRCInstantiate(particlePrefab);
        particleObj.transform.SetParent(transform, false);
        particleObj.transform.localPosition = Vector3.zero;
        
        // ParticleSystemコンポーネントを取得
        particleSystem = particleObj.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            Debug.LogError("[Poeticle] プレハブにParticleSystemコンポーネントがありません");
            return;
        }
        
        // ParticleSystemRendererを取得
        particleRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
        {
            Debug.LogError("[Poeticle] ParticleSystemRendererが見つかりません");
            return;
        }
        
        // マテリアルを設定
        if (textMaterial != null)
        {
            particleRenderer.material = textMaterial;
        }
        
        // ParticleSystemの設定
        var main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // 寿命設定
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        
        // サイズ設定
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        
        // 回転設定
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        
        // 色設定
        main.startColor = textColor;
        
        // Emissionモジュール設定
        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0; // 手動でパーティクルを発生させる
        
        // シェイプモジュール設定
        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spawnRadius;
        shape.radiusThickness = 1.0f; // フルスフィア
        
        // 動きのモジュール設定
        var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = floatSpeed; // 上方向への動き
        
        // 回転のモジュール設定
        var rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = rotationSpeed * Mathf.Deg2Rad; // 度からラジアンに変換
        
        // テクスチャシートアニメーション設定（テキスト用）
        if (textureAtlas != null)
        {
            var textureSheet = particleSystem.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = 8; // テクスチャアトラスの列数（調整が必要）
            textureSheet.numTilesY = 8; // テクスチャアトラスの行数（調整が必要）
        }
        
        // パーティクルを停止状態にする
        particleSystem.Stop(true);
    }
    
    public void EmitTextParticles()
    {
        if (particleSystem == null || textLineCount == 0)
        {
            return;
        }
        
        // 既存のパーティクルをクリア
        particleSystem.Clear();
        
        // テキスト行ごとにパーティクルを放出
        int emitCount = Mathf.Min(textLineCount, maxParticles);
        
        // パーティクルをエミットする準備
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        
        for (int i = 0; i < emitCount; i++)
        {
            // ランダム位置を設定
            Vector3 position = transform.position + UnityEngine.Random.insideUnitSphere * spawnRadius;
            emitParams.position = position;
            
            // ランダムサイズを設定
            float size = UnityEngine.Random.Range(sizeMin, sizeMax);
            emitParams.startSize = size;
            
            // ランダム寿命を設定
            float lifetime = UnityEngine.Random.Range(lifetimeMin, lifetimeMax);
            emitParams.startLifetime = lifetime;
            
            // ランダム回転設定（EmitParamsにはstartRotationがないのでメインモジュールで設定）
            // 注：個別のパーティクルごとの回転は設定できないので、全体の設定を使用
            
            // テクスチャタイルインデックスの設定（文字に対応）
            // 注: 実際のテクスチャアトラスの配置に合わせて調整が必要
            int tileIndex = i % 64; // 8x8のテクスチャアトラスを想定
            
            // UdonSharpの制限に対応するため、ローテーションインデックスを使用
            emitParams.rotation3D = Vector3.forward * UnityEngine.Random.Range(0f, 360f);
            
            // テキストごとに若干色を変える（オプション）
            emitParams.startColor = new Color(
                textColor.r * UnityEngine.Random.Range(0.9f, 1.0f),
                textColor.g * UnityEngine.Random.Range(0.9f, 1.0f),
                textColor.b * UnityEngine.Random.Range(0.9f, 1.0f),
                textColor.a
            );
            
            // パーティクル放出
            particleSystem.Emit(emitParams, 1);
        }
        
        // パーティクルシステムを再生
        particleSystem.Play();
    }
    
    // プレイヤーがトリガーに入ったときに再生（オプション）
    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player.isLocal && particleSystem != null)
        {
            EmitTextParticles();
        }
    }
    
    // パーティクルをリセットするパブリックメソッド（インタラクション用）
    public void ResetParticles()
    {
        if (particleSystem != null)
        {
            EmitTextParticles();
        }
    }
    
    // インタラクション用のトグルメソッド
    public void ToggleParticles()
    {
        if (particleSystem == null) return;
        
        if (particleSystem.isPlaying)
        {
            particleSystem.Stop(true);
        }
        else
        {
            EmitTextParticles();
        }
    }
}