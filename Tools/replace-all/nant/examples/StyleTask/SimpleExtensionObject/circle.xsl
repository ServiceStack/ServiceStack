<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:myObj="urn:Calculate">

  <xsl:template match="data">
  <circles>
  <xsl:for-each select="circle">
    <circle>
    <xsl:copy-of select="node()"/>
       <circumference>
          <xsl:value-of select="Calculate:Circumference(radius)"/>        
       </circumference>
    </circle>
  </xsl:for-each>
  </circles>
  </xsl:template>
</xsl:stylesheet>